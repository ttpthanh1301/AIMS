# ML CV Screening Guide

## Overview
Machine learning model cho phép tự động chấm điểm CV ứng viên dựa trên kỹ năng, GPA, kinh nghiệm và độ tương đồng với yêu cầu công việc.

## Model Status
- **Backend URL**: `http://localhost:5291`
- **Model path**: `src/AIMS.BackendServer/wwwroot/ml/screening_model.zip`
- **Latest training**:
  - Accuracy: 71%
  - F1 Score: 83%
  - AUC: 54%

## Quick Start

### 1. Check model status
```bash
curl http://localhost:5291/api/admin/screening/status
# Response: {"modelLoaded":true}
```

### 2. Train/Retrain model
Convert dataset → features CSV → train model immediately:
```bash
curl -X POST "http://localhost:5291/api/admin/screening/convert-to-features" \
  -H "Content-Type: application/json" \
  -d '{
    "inputPath":"/Users/tranthanh/Documents/Workspaces/AIMS/src/AIMS.BackendServer/wwwroot/datatest/resume_dataset_200k_enhanced (1).csv",
    "outputPath":"/Users/tranthanh/Documents/Workspaces/AIMS/src/AIMS.BackendServer/wwwroot/datatest/resume_features.csv",
    "trainImmediately":true
  }'
```

### 3. Use ML in Web UI
- Go to `http://localhost:5291/HR/Screening` (WebPortal)
- Upload CV or batch process → Backend will use ML model for scoring
- Scores returned in `matchingScore` field

## Admin Endpoints

### POST `/api/admin/screening/train`
Train model from feature CSV
```bash
curl -X POST "http://localhost:5291/api/admin/screening/train" \
  -H "Content-Type: application/json" \
  -d '{"CsvPath":"/path/to/resume_features.csv"}'
```

### POST `/api/admin/screening/export`
Export single CV → features CSV (for incremental dataset building)
```bash
curl -X POST "http://localhost:5291/api/admin/screening/export" \
  -H "Content-Type: application/json" \
  -d '{
    "CsvPath":"/path/to/export.csv",
    "CvText":"Your CV text...",
    "JobDescription":"JD text...",
    "Label":true,
    "CandidateGpa":8.5
  }'
```

### POST `/api/admin/screening/normalize-csv`
Normalize labels (0/1 → true/false) and GPA (to 4.0 scale)
```bash
curl -X POST "http://localhost:5291/api/admin/screening/normalize-csv" \
  -H "Content-Type: application/json" \
  -d '{
    "inputPath":"/path/to/raw.csv",
    "outputPath":"/path/to/normalized.csv"
  }'
```

### POST `/api/admin/screening/convert-to-features`
Convert raw resume CSV → feature CSV (optionally train immediately)
```bash
curl -X POST "http://localhost:5291/api/admin/screening/convert-to-features" \
  -H "Content-Type: application/json" \
  -d '{
    "inputPath":"/path/to/resume_dataset.csv",
    "outputPath":"/path/to/resume_features.csv",
    "trainImmediately":true
  }'
```

### GET `/api/admin/screening/status`
Check if model is loaded
```bash
curl http://localhost:5291/api/admin/screening/status
# {"modelLoaded":true}
```

## Feature Schema
Model expects CSV with these columns:
1. `SkillsMatchedCount` (float) — matched skills from CV vs JD
2. `MissingSkillsCount` (float) — missing skills (currently 0)
3. `Gpa` (float) — GPA on 4.0 scale
4. `YearsOfExperience` (float) — years of experience
5. `CosineSimilarity` (float) — TF-IDF cosine similarity (0-1)
6. `Label` (true/false) — hired or not (for training only)

## Key Files
- **Model code**: `src/AIMS.BackendServer/Services/ML/`
  - `ScreeningModelService.cs` — Train/predict logic
  - `FeatureData.cs` — Feature schema
  - `FeatureCsvConverter.cs` — CSV conversion logic
  - `FeatureExporter.cs` — Append features to CSV
  - `CsvNormalizer.cs` — Label/GPA normalization

- **Backend controller**: `src/AIMS.BackendServer/Controllers/ML/ScreeningModelController.cs`
- **WebPortal screening**: `src/AIMS.WebPortal/Areas/HR/Controllers/ScreeningController.cs`
- **Dataset**: `src/AIMS.BackendServer/wwwroot/datatest/resume_dataset_200k_enhanced (1).csv`

## Integration Points
1. **HR/Screening UI** → calls `/api/screening/batch/{jdId}` → backend uses ML if model available
2. **Single CV screening** → POST `/api/screening/{applicationId}` → ML prediction + feature export
3. **Periodic retrain** → Background job `ScreeningAutoRetrainer` (disabled by default, config: `ScreeningSettings:EnableAutoRetrain`)

## Notes
- Current features are simplified (no raw CV/JD text → TF-IDF is approximated via skills_score normalization)
- To improve accuracy: add more features (certifications, projects, university tier) or implement true TF-IDF from raw text
- Model saved as `screening_model.zip` (binary ML.NET format) — loads at backend startup if exists
