from __future__ import annotations

import asyncio
import json
import logging
import os
from uuid import uuid4

from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse

from .config import get_settings
from .evaluator import evaluate_metric
from .models import CapabilitiesResponse, EvaluationRequest, EvaluationResponse, HealthResponse, SUPPORTED_METRICS


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        value = {"level": record.levelname, "message": record.getMessage(), "logger": record.name}
        for name in ("correlation_id", "request_id", "metric_name"):
            if hasattr(record, name): value[name] = getattr(record, name)
        return json.dumps(value, ensure_ascii=False)


handler = logging.StreamHandler()
handler.setFormatter(JsonFormatter())
logging.basicConfig(level=os.getenv("RAGAS_LOG_LEVEL", "INFO").upper(), handlers=[handler], force=True)
logger = logging.getLogger("educhatai.ragas")
app = FastAPI(title="EduChatAI Ragas Evaluation Service", version="1.0.0")


@app.middleware("http")
async def correlation(request: Request, call_next):
    correlation_id = request.headers.get("x-correlation-id") or str(uuid4())
    request.state.correlation_id = correlation_id
    try: response = await call_next(request)
    except Exception:
        logger.exception("Unhandled request failure", extra={"correlation_id": correlation_id})
        response = JSONResponse(status_code=500, content={"detail": "The evaluation service encountered an unexpected error."})
    response.headers["x-correlation-id"] = correlation_id
    return response


@app.get("/health", response_model=HealthResponse)
async def health() -> HealthResponse:
    return HealthResponse()


@app.get("/v1/capabilities", response_model=CapabilitiesResponse)
async def capabilities() -> CapabilitiesResponse:
    settings = get_settings()
    return CapabilitiesResponse(metrics=list(SUPPORTED_METRICS), llm_options=list(settings.llm_options), embedding_options=list(settings.embedding_options))


@app.post("/v1/evaluations", response_model=EvaluationResponse)
async def evaluate(request: EvaluationRequest, http_request: Request) -> EvaluationResponse:
    try: get_settings().validate_selection(request.llm_provider, request.llm_model, request.embedding_provider, request.embedding_model)
    except ValueError as exception: raise HTTPException(status_code=422, detail=str(exception)) from exception
    logger.info("Evaluation started", extra={"correlation_id": http_request.state.correlation_id, "request_id": str(request.request_id)})
    results = await asyncio.gather(*(evaluate_metric(request, metric_name) for metric_name in request.metrics))
    logger.info("Evaluation completed", extra={"correlation_id": http_request.state.correlation_id, "request_id": str(request.request_id)})
    return EvaluationResponse(request_id=request.request_id, results=list(results))
