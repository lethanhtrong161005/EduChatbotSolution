from __future__ import annotations

import math
import os
from functools import lru_cache
from time import perf_counter
from typing import Any

import instructor
import litellm
from ragas.embeddings.base import embedding_factory
from ragas.llms.base import llm_factory
from ragas.metrics.collections import AnswerRelevancy, ContextPrecision, ContextRecall, Faithfulness

from .models import EvaluationRequest, MetricResult

VIETNAMESE_EVALUATOR_SYSTEM_PROMPT = """Bạn là bộ đánh giá RAG tiếng Việt có tính nhất quán. Hãy giữ nguyên ý nghĩa của câu hỏi, câu trả lời tham chiếu, câu trả lời được sinh và ngữ cảnh; đánh giá dựa trên bằng chứng được cung cấp, không bổ sung kiến thức bên ngoài. Trả về đúng cấu trúc mà hệ thống yêu cầu."""


def _qualified_model(provider: str, model: str) -> str:
    return model if model.startswith(f"{provider}/") else f"{provider}/{model}"


def _provider_args(provider: str) -> dict[str, Any]:
    return {"api_base": os.getenv("OLLAMA_API_BASE", "http://localhost:11434")} if provider == "ollama" else {}


@lru_cache(maxsize=32)
def _components(llm_provider: str, llm_model: str, embedding_provider: str, embedding_model: str) -> tuple[Any, Any]:
    client = instructor.from_litellm(litellm.acompletion, mode=instructor.Mode.JSON)
    llm = llm_factory(_qualified_model(llm_provider, llm_model), provider="litellm", client=client, adapter="litellm", temperature=0, top_p=0.1, max_tokens=4096, system_prompt=VIETNAMESE_EVALUATOR_SYSTEM_PROMPT, **_provider_args(llm_provider))
    embeddings = embedding_factory("litellm", model=_qualified_model(embedding_provider, embedding_model), interface="modern", timeout=180, max_retries=0, **_provider_args(embedding_provider))
    return llm, embeddings


def _metric(metric_name: str, llm: Any, embeddings: Any) -> Any:
    if metric_name == "faithfulness": return Faithfulness(llm=llm)
    if metric_name == "answer_relevancy": return AnswerRelevancy(llm=llm, embeddings=embeddings)
    if metric_name == "context_precision": return ContextPrecision(llm=llm)
    if metric_name == "context_recall": return ContextRecall(llm=llm)
    raise ValueError(f"unsupported metric '{metric_name}'")


async def evaluate_metric(request: EvaluationRequest, metric_name: str) -> MetricResult:
    started = perf_counter()
    try:
        llm, embeddings = _components(request.llm_provider, request.llm_model, request.embedding_provider, request.embedding_model)
        metric = _metric(metric_name, llm, embeddings)
        if metric_name == "faithfulness": result = await metric.ascore(user_input=request.question, response=request.response, retrieved_contexts=request.prompt_contexts)
        elif metric_name == "answer_relevancy": result = await metric.ascore(user_input=request.question, response=request.response)
        elif metric_name == "context_precision": result = await metric.ascore(user_input=request.question, reference=request.reference, retrieved_contexts=request.retrieval_contexts)
        else: result = await metric.ascore(user_input=request.question, retrieved_contexts=request.retrieval_contexts, reference=request.reference)
        score = float(result.value)
        if not math.isfinite(score) or not 0 <= score <= 1: raise ValueError(f"metric returned invalid score '{score}'")
        return MetricResult(metric_name=metric_name, status="completed", score=score, reason=result.reason, duration_ms=round((perf_counter() - started) * 1000))
    except Exception as exception:
        return MetricResult(metric_name=metric_name, status="failed", error_code="invalid_score" if "invalid score" in str(exception) else "evaluation_error", error_message=str(exception)[:2000], duration_ms=round((perf_counter() - started) * 1000))
