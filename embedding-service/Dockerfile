FROM python:3.11-slim

WORKDIR /app

ENV PIP_DEFAULT_TIMEOUT=300 \
    PIP_RETRIES=10 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PYTHONUNBUFFERED=1 \
    HF_HOME=/models \
    HF_HUB_DOWNLOAD_TIMEOUT=300

# 1) CPU-only PyTorch (~200 MB instead of ~2.5 GB with CUDA libraries).
#    Its own layer, so a retry after a network failure does not redo finished layers.
RUN pip install --extra-index-url https://download.pytorch.org/whl/cpu torch==2.4.1

# 2) Everything else. torch is already satisfied, so it is not downloaded again.
COPY requirements.txt .
RUN pip install -r requirements.txt

# 3) Bake the model into the image so container start-up needs no internet.
RUN python -c "from sentence_transformers import SentenceTransformer; SentenceTransformer('sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2')"

COPY main.py .

EXPOSE 8000
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
