from __future__ import annotations

import math

import pytest

from app import evaluator
from app.models import EvaluationRequest


class Result:
    def __init__(self, value, reason=None): self.value, self.reason = value, reason


class FakeMetric:
    def __init__(self, value=.75): self.value, self.calls = value, []
    async def ascore(self, **kwargs): self.calls.append(kwargs); return Result(self.value, "reason")


def request(metric):
    return EvaluationRequest(request_id="5bb58704-6b76-4f4f-a704-ae26ec40b609", contract_version="ragas-evaluation-v1", language="vi", question="question", reference="reference", response="response", prompt_contexts=["prompt"], retrieval_contexts=["rank 1", "rank 2"], metrics=[metric], llm_provider="gemini", llm_model="gemini-2.5-flash", embedding_provider="gemini", embedding_model="gemini-embedding-001", prompt_version="vi-ragas-v1")


@pytest.mark.parametrize(("metric_name", "expected"), [
    ("faithfulness", {"user_input": "question", "response": "response", "retrieved_contexts": ["prompt"]}),
    ("answer_relevancy", {"user_input": "question", "response": "response"}),
    ("context_precision", {"user_input": "question", "reference": "reference", "retrieved_contexts": ["rank 1", "rank 2"]}),
    ("context_recall", {"user_input": "question", "reference": "reference", "retrieved_contexts": ["rank 1", "rank 2"]}),
])
async def test_metric_specific_evidence(monkeypatch, metric_name, expected):
    metric = FakeMetric()
    monkeypatch.setattr(evaluator, "_components", lambda *_: (object(), object()))
    monkeypatch.setattr(evaluator, "_metric", lambda *_: metric)
    result = await evaluator.evaluate_metric(request(metric_name), metric_name)
    assert result.status == "completed" and result.score == .75 and metric.calls == [expected]


@pytest.mark.parametrize("value", [math.nan, math.inf, -0.1, 1.1])
async def test_invalid_metric_score_is_rejected(monkeypatch, value):
    monkeypatch.setattr(evaluator, "_components", lambda *_: (object(), object()))
    monkeypatch.setattr(evaluator, "_metric", lambda *_: FakeMetric(value))
    result = await evaluator.evaluate_metric(request("faithfulness"), "faithfulness")
    assert result.status == "failed" and result.score is None and result.error_code == "invalid_score"


def test_vietnamese_evaluator_prompt_is_versioned_and_nonempty():
    assert "tiếng Việt" in evaluator.VIETNAMESE_EVALUATOR_SYSTEM_PROMPT
    assert evaluator.VIETNAMESE_EVALUATOR_SYSTEM_PROMPT.strip()


@pytest.mark.parametrize(("provider", "model", "expected"), [("ollama", "qwen3", "ollama/qwen3"), ("openrouter", "google/gemini-2.5-flash", "openrouter/google/gemini-2.5-flash"), ("gemini", "gemini/gemini-2.5-flash", "gemini/gemini-2.5-flash")])
def test_model_identifiers_are_qualified_for_litellm(provider, model, expected):
    assert evaluator._qualified_model(provider, model) == expected
