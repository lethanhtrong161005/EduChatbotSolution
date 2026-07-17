FROM python:3.12-slim AS service

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PIP_NO_CACHE_DIR=1

WORKDIR /app
COPY RagasService/pyproject.toml ./
COPY RagasService/app ./app
RUN pip install .

EXPOSE 8090
HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=5 CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8090/health', timeout=3)"
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8090"]

FROM service AS test
COPY RagasService/tests ./tests
RUN pip install '.[test]'
CMD ["pytest", "tests", "-q"]

FROM service AS runtime
