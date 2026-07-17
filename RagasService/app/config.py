from __future__ import annotations

import json
import os
from functools import lru_cache

from .models import ModelOption

DEFAULT_LLM_OPTIONS = '[{"provider":"gemini","model":"gemini-2.5-flash","label":"Gemini 2.5 Flash"},{"provider":"ollama","model":"qwen3","label":"Qwen 3 · Ollama"}]'
DEFAULT_EMBEDDING_OPTIONS = '[{"provider":"gemini","model":"gemini-embedding-001","label":"Gemini Embedding 001"},{"provider":"ollama","model":"bge-m3","label":"BGE-M3 · Ollama"}]'


class Settings:
    def __init__(self) -> None:
        self.llm_options = self._options("RAGAS_LLM_OPTIONS", DEFAULT_LLM_OPTIONS)
        self.embedding_options = self._options("RAGAS_EMBEDDING_OPTIONS", DEFAULT_EMBEDDING_OPTIONS)

    @staticmethod
    def _options(name: str, fallback: str) -> tuple[ModelOption, ...]:
        value = json.loads(os.getenv(name, fallback))
        options = tuple(ModelOption.model_validate(item) for item in value)
        if not options:
            raise ValueError(f"{name} must contain at least one configured option")
        identities = {(option.provider, option.model) for option in options}
        if len(identities) != len(options):
            raise ValueError(f"{name} cannot contain duplicate provider/model identities")
        return options

    def validate_selection(self, llm_provider: str, llm_model: str, embedding_provider: str, embedding_model: str) -> None:
        if not any(option.provider == llm_provider and option.model == llm_model for option in self.llm_options):
            raise ValueError("selected evaluator LLM is not in the configured capability allowlist")
        if not any(option.provider == embedding_provider and option.model == embedding_model for option in self.embedding_options):
            raise ValueError("selected evaluator embedding model is not in the configured capability allowlist")


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
