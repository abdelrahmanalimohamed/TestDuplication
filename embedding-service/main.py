"""Tiny embedding sidecar for the .NET API.

Model: paraphrase-multilingual-MiniLM-L12-v2 (384 dims, 50+ languages incl. Arabic + English).
Swap MODEL_NAME for a bigger model (e.g. BAAI/bge-m3 = 1024 dims) and update Qdrant:Dimensions, then call /api/admin/reindex.
"""
import os
from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer

MODEL_NAME = os.getenv("MODEL_NAME", "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2")

model = SentenceTransformer(MODEL_NAME)
app = FastAPI(title="Embedding service")


class EmbedRequest(BaseModel):
    texts: list[str]


@app.post("/embed")
def embed(request: EmbedRequest):
    vectors = model.encode(request.texts, normalize_embeddings=True, batch_size=64)
    return {"embeddings": vectors.tolist()}


@app.get("/health")
def health():
    return {"status": "ok", "model": MODEL_NAME}
