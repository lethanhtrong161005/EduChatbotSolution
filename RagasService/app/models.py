from __future__ import annotations

import math
from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

CONTRACT_VERSION = "ragas-evaluation-v1"
SERVICE_VERSION = "1.0.0"
RAGAS_VERSION = "0.4.3"
PROMPT_VERSION = "vi-ragas-v1"
SUPPORTED_METRICS = ("faithfulness", "answer_relevancy", "context_precision", "context_recall")


class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid")


class ModelOption(StrictModel):
    provider: str = Field(min_length=1)
    model: str = Field(min_length=1)
    label: str = Field(min_length=1)


class HealthResponse(StrictModel):
    status: Literal["healthy"] = "healthy"
    contract_version: str = CONTRACT_VERSION
    service_version: str = SERVICE_VERSION
    ragas_version: str = RAGAS_VERSION


class CapabilitiesResponse(StrictModel):
    contract_version: str = CONTRACT_VERSION
    service_version: str = SERVICE_VERSION
    ragas_version: str = RAGAS_VERSION
    prompt_version: str = PROMPT_VERSION
    metrics: list[str]
    llm_options: list[ModelOption]
    embedding_options: list[ModelOption]


class EvaluationRequest(StrictModel):
    request_id: UUID
    contract_version: str
    language: str = Field(min_length=1)
    question: str = Field(min_length=1)
    reference: str = Field(min_length=1)
    response: str = Field(min_length=1)
    prompt_contexts: list[str]
    retrieval_contexts: list[str]
    metrics: list[str] = Field(min_length=1)
    llm_provider: str = Field(min_length=1)
    llm_model: str = Field(min_length=1)
    embedding_provider: str = Field(min_length=1)
    embedding_model: str = Field(min_length=1)
    prompt_version: str

    @field_validator("prompt_contexts", "retrieval_contexts")
    @classmethod
    def contexts_must_be_nonempty_strings(cls, value: list[str]) -> list[str]:
        if any(not context.strip() for context in value):
            raise ValueError("contexts cannot contain empty values")
        return value

    @field_validator("metrics")
    @classmethod
    def metrics_must_be_known_and_unique(cls, value: list[str]) -> list[str]:
        unknown = [metric for metric in value if metric not in SUPPORTED_METRICS]
        if unknown:
            raise ValueError(f"unsupported metrics: {', '.join(unknown)}")
        if len(value) != len(set(value)):
            raise ValueError("metrics cannot contain duplicates")
        return value

    @model_validator(mode="after")
    def contract_and_prompt_versions_must_match(self) -> EvaluationRequest:
        if self.contract_version != CONTRACT_VERSION:
            raise ValueError(f"unsupported contract version '{self.contract_version}'")
        if self.prompt_version != PROMPT_VERSION:
            raise ValueError(f"unsupported prompt version '{self.prompt_version}'")
        return self


class MetricResult(StrictModel):
    metric_name: str
    status: Literal["completed", "failed"]
    score: float | None = None
    reason: str | None = None
    error_code: str | None = None
    error_message: str | None = None
    duration_ms: int = Field(ge=0)

    @model_validator(mode="after")
    def score_must_match_status(self) -> MetricResult:
        if self.status == "completed" and (self.score is None or not math.isfinite(self.score) or not 0 <= self.score <= 1):
            raise ValueError("completed metric score must be finite and between 0 and 1")
        if self.status == "failed" and self.score is not None:
            raise ValueError("failed metric cannot contain a score")
        return self


class EvaluationResponse(StrictModel):
    request_id: UUID
    contract_version: str = CONTRACT_VERSION
    service_version: str = SERVICE_VERSION
    ragas_version: str = RAGAS_VERSION
    prompt_version: str = PROMPT_VERSION
    results: list[MetricResult]
