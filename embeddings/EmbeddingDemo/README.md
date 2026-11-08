# AI Espresso — Text Embeddings Demo

[![Embeddings - From Text to Vector](./assets/From%20Text%20to%20Vectors.png)](https://youtu.be/ID4Flt5HvrA)

This repository contains the C# sample code for [Embeddings - From Text to Vector](https://youtu.be/ID4Flt5HvrA) on [AI Espresso (@aiespresso)](https://www.youtube.com/@aiespresso).

Search four short helpdesk documents with a text embedding and cosine similarity.
The original example compares an exact `laptop` keyword filter with semantic ranking.

## Requirements

- .NET 10 SDK
- An OpenAI API key with access to the configured embedding model and available API usage
- Internet access to `https://api.openai.com`

No additional NuGet packages are required. Model selection is centralized in
`appsettings.json`. This example uses the **Embeddings API**, independently of the
Responses API.

## Run in PowerShell

Open a terminal inside this extracted `EmbeddingDemo` folder. Set the key in your
own terminal before recording; do not show the credential on screen or commit it.

```powershell
$env:OPENAI_API_KEY = "YOUR_API_KEY"
dotnet build
dotnet run
```

On bash/zsh, set the environment variable with `export OPENAI_API_KEY="YOUR_API_KEY"`.
If you set the environment variable persistently outside VS Code, restart the
VS Code terminal so it receives the updated environment.

## What the program does

1. Loads the model setting and API key.
2. Shows documents containing the literal word `laptop` (document B).
3. Sends all four documents to `/v1/embeddings` in one request.
4. Sends the query to the same endpoint with the same model in a second request.
5. Reads each response's `data[].index` and `data[].embedding`.
6. Displays the query vector length and its first five values.
7. Compares **complete** vectors with cosine similarity and sorts the documents.

Each run makes two API requests. For clarity, it regenerates document vectors on
each run. A larger application would usually persist document embeddings and
regenerate them when documents or the embedding configuration change.

## Output interpretation

The default query is `My laptop won't turn on.` The intended relevant document is
A: `A computer fails to start when the power button is pressed.` B contains the
word `laptop` but concerns travel accessories. Confirm the actual ranking before
recording. Scores and rankings must come from your run; no real API result or
fixed similarity score is bundled here.

The default configured model currently returns 1,536 dimensions when no custom
dimension count is requested. The program prints the actual length rather than
assuming it. Vector length is independent of word count or token count for these
valid input texts. The first five displayed values are only a preview.

A higher cosine score indicates more similar vectors in this comparison. A score
of 0.8 does **not** mean an 80% probability that a document answers the question.
Even the highest-ranked document may be insufficient. There is no universal
relevance threshold established by this four-document example.

Use the same model and compatible settings for every compared vector. Equal
dimensions alone do not make vectors from different models comparable.

## Optional second query

```powershell
dotnet run -- "I forgot my password."
```

Check whether C is now the most related result. The fixed keyword baseline still
uses `laptop`; it belongs to the original example and is not a keyword benchmark
for arbitrary queries. Skip that part when presenting this extension.

## Recording map

- Start with `dotnet run` and show the result.
- Step 1: query, documents, `appsettings.json`, and environment-variable key.
- Step 3 and `GenerateEmbeddingsAsync`: model, input, returned index and vector.
- Terminal: actual dimensions and the five-value preview.
- Step 4: `CosineSimilarity` and `OrderByDescending`.
- Fold the formula helper; explain its mathematics in the next episode.

Class and method summaries use C# XML documentation comments. Numbered step
comments are intended to help align the code walkthrough with the narration.

## Troubleshooting

- Missing key: set `OPENAI_API_KEY` in the terminal running the application.
- HTTP 401: verify the API credential.
- HTTP 403/404: verify project permissions and the configured model's availability.
- HTTP 429: check API quota, billing, or rate limits before rerunning.
- Timeout: verify connectivity, then rerun when the service is reachable.
- Missing configuration file: keep `appsettings.json` with the project; building
  copies it to the output folder automatically.

Errors produce a nonzero exit code. The program does not log the API key or raw
request/response bodies.

## Verification

Built with .NET SDK 10.0.403: zero warnings and zero errors. Additional offline
checks cover cosine calculations, rejected invalid vectors, and shuffled response
indices. Real OpenAI calls and semantic rankings have **not** been run in the
authoring environment; use your own API key for the recording rehearsal.

## Official references

- [Vector embeddings](https://developers.openai.com/api/docs/guides/embeddings)
- [Create embeddings](https://developers.openai.com/api/reference/resources/embeddings/methods/create)

API documentation checked November 6, 2026. No generated answers, vector database,
or RAG pipeline are included in this focused retrieval example.
