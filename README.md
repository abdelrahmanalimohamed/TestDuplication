# Item Deduplication

Finds items that are the same in the real world even when code, language, spelling or units differ
(`Vacuum 60 cm` = `مكنسة ٦٠ سم`).

## How it works (matches your diagram)

```
Import / Create item
      │
      ▼
Normalization   Arabic letters, digits ٦٠→60, diacritics, punctuation, 60cm→60 cm, 2.5L
      │
      ▼
Matching engine (3 signals, one score 0..1)
   ├─ Text       FuzzySharp          typos, word order        (skipped when Arabic vs English)
   ├─ Semantic   multilingual model  "vacuum" ≈ "مكنسة"      → Qdrant
   └─ Attributes number + unit       60 cm == ٦٠ سم, 60 cm ≠ 80 cm (conflict = penalty)
      │
      ▼
Decision
   ≥ 0.98  auto-link as alias (configurable / can be disabled)
   ≥ 0.65  DuplicateCandidate → human Approve (merge) / Reject (new master)
   <  0.65  new master item
      │
      ▼
SQL Server: MasterItem · ItemAlias (all codes/names) · DuplicateCandidate · AuditLog
Qdrant:     one vector per alias (rebuildable from SQL with /api/admin/reindex)
```

SQL is the source of truth; Qdrant is a rebuildable index. If the embedding service or Qdrant is down,
matching degrades to text + attributes instead of failing.

## Run it

```bash
./setup.sh                      # creates the .sln and restores packages
docker compose up -d            # SQL Server, Qdrant, embedding service (first build downloads the model)
dotnet run --project src/ItemDeduplication.Api     # Swagger at http://localhost:5080/swagger
dotnet test
```

Try it in Swagger: `POST /api/items/import-bulk` with `sample-data.json`, then `GET /api/duplicates/pending`.
Expected: `Vacuum 80 cm` becomes its own item (size conflict), `مكنسة ٦٠ سم` and `زجاجة ماء ١٫٥ لتر` are flagged
against their English twins.

## Troubleshooting

**`pip ... ReadTimeoutError` while building the embedding image:** the Dockerfile installs CPU-only PyTorch to keep
the download small. If your connection is still unstable, just re-run `docker compose build embedding`
(finished layers are cached, so it resumes where it stopped).

## Endpoints

| Endpoint | Purpose |
|---|---|
| `POST /api/items/check` `{name}` | Read-only "similar items?" for the create-item screen |
| `POST /api/items/import` | Import one item (create / auto-link / flag) |
| `POST /api/items/import-bulk` | Import a list (ERP/SAP/Excel extract) |
| `GET  /api/duplicates/pending` | Review queue, highest confidence first |
| `POST /api/duplicates/{id}/approve` | Same item → becomes an alias of the master |
| `POST /api/duplicates/{id}/reject` | Different item → new master item |
| `POST /api/admin/reindex` | Rebuild Qdrant from SQL |

## Changes to your normalizer

All your existing tests still pass. Three fixes, each needed by the matching step:

1. `2.5 L` used to become `2 5 l` (the dot was a separator). Decimal points inside numbers are now kept, and `٫` → `.`.
2. `60cm` now becomes `60 cm`, so it tokenises like `60 cm`.
3. `60x40` now becomes `60 40`.

Normalization alone can never match English to Arabic. That is the job of the embedding model, and of the
unit extractor for the numbers.

## Tuning (important)

The weights and thresholds in `appsettings.json → Matching` are sensible starting points, not truths.

1. Export 200-500 real pairs from your data and label them duplicate / not duplicate.
2. Run `/api/items/check` on them and look at the scores.
3. Adjust `ReviewThreshold`, `HighConfidenceThreshold`, `SemanticFloor/Ceiling` until the review queue is useful.
4. Keep `AutoLinkThreshold` high (or > 1.0 to disable) until you trust the results.

## Known limits / next steps

- Candidate lookup uses `LIKE '%token%'`: fine to ~100k aliases. Beyond that add SQL Server Full-Text Search.
- Extend `UnitCatalog` with your domain units; add a brand/model-number extractor (a model number mismatch should be a hard conflict).
- Add a synonym dictionary for abbreviations (`vac`, `ss` = stainless steel).
- Larger/better embedding model (`BAAI/bge-m3`): change `MODEL_NAME`, `Qdrant:Dimensions`, then reindex.
- Add authentication and take `Reviewer` from the logged-in user.
- Use EF migrations instead of `EnsureCreated` in production.
- pgvector: implement `IVectorStore` against PostgreSQL; nothing else changes.
