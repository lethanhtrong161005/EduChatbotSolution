from __future__ import annotations

from uuid import uuid4

from fastapi.testclient import TestClient

from app.main import app
from app.models import MetricResult


def request(**overrides):
    value = {
        "request_id": str(uuid4()), "contract_version": "ragas-evaluation-v1", "language": "vi", "question": "Câu hỏi?", "reference": "Tham chiếu.", "response": "Câu trả lời.",
        "prompt_contexts": ["Ngữ cảnh trong prompt."], "retrieval_contexts": ["Ngữ cảnh trong prompt.", "Ngữ cảnh chỉ truy xuất."], "metrics": ["faithfulness", "answer_relevancy"],
        "llm_provider": "gemini", "llm_model": "gemini-2.5-flash", "embedding_provider": "gemini", "embedding_model": "gemini-embedding-001", "prompt_version": "vi-ragas-v1",
    }
    value.update(overrides)
    return value


def test_health_and_capabilities_expose_versioned_contract_and_allowlist():
    client = TestClient(app)
    health = client.get("/health")
    capabilities = client.get("/v1/capabilities")
    assert health.status_code == 200 and health.json() == {"status": "healthy", "contract_version": "ragas-evaluation-v1", "service_version": "1.0.0", "ragas_version": "0.4.3"}
    assert capabilities.status_code == 200
    assert capabilities.json()["prompt_version"] == "vi-ragas-v1"
    assert capabilities.json()["metrics"] == ["faithfulness", "answer_relevancy", "context_precision", "context_recall"]
    assert {f'{option["provider"]}/{option["model"]}' for option in capabilities.json()["llm_options"]} == {"gemini/gemini-2.5-flash", "ollama/qwen3"}
    assert {f'{option["provider"]}/{option["model"]}' for option in capabilities.json()["embedding_options"]} == {"gemini/gemini-embedding-001", "ollama/bge-m3"}


def test_evaluation_returns_independent_metric_results(monkeypatch):
    async def fake_evaluate(request, metric_name):
        return MetricResult(metric_name=metric_name, status="completed" if metric_name == "faithfulness" else "failed", score=.8 if metric_name == "faithfulness" else None, error_code=None if metric_name == "faithfulness" else "evaluation_error", error_message=None if metric_name == "faithfulness" else "failed independently", duration_ms=10)
    monkeypatch.setattr("app.main.evaluate_metric", fake_evaluate)
    response = TestClient(app).post("/v1/evaluations", json=request())
    assert response.status_code == 200
    assert response.json()["results"] == [
        {"metric_name": "faithfulness", "status": "completed", "score": .8, "reason": None, "error_code": None, "error_message": None, "duration_ms": 10},
        {"metric_name": "answer_relevancy", "status": "failed", "score": None, "reason": None, "error_code": "evaluation_error", "error_message": "failed independently", "duration_ms": 10},
    ]


def test_evaluation_rejects_unknown_capability_selection():
    response = TestClient(app).post("/v1/evaluations", json=request(llm_model="not-allowed"))
    assert response.status_code == 422
    assert "capability allowlist" in response.json()["detail"]


def test_evaluation_rejects_unknown_duplicate_metrics_and_contract_drift():
    client = TestClient(app)
    assert client.post("/v1/evaluations", json=request(metrics=["unknown"])).status_code == 422
    assert client.post("/v1/evaluations", json=request(metrics=["faithfulness", "faithfulness"])).status_code == 422
    assert client.post("/v1/evaluations", json=request(contract_version="ragas-evaluation-v2")).status_code == 422
