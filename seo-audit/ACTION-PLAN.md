# FIBRADIS — SEO Action Plan

**Site:** https://fibrasinmobiliarias.com/ · **Date:** 2026-06-13 · **Health Score:** 84/100

Priority order: **Critical → High → Medium → Low**. Effort: S (<2h), M (½–1 day), L (multi-day).

---

## 🔴 Critical — fix immediately

### C1. Clean FIBRA-profile meta descriptions (S–M) — *biggest SERP-quality win*
The description for `/fibras/*` is a raw markdown dump: `" ?? Fibra Uno | FUNO11 Ticker: FUNO11 ... | Campo | Detalle | ..."`. It feeds three surfaces: `<meta name="description">`, `twitter:description`, and `FinancialProduct.description` (JSON-LD).
- **Fix:** in the metadata middleware (`FibraProfileMetadataMiddleware.cs`), generate the description from a clean field (e.g. the "Descripción" long-form prose first sentence, or a templated summary: *"Análisis de {Nombre} ({Ticker}): precio, yield {yield}%, fundamentales (Cap Rate, NAV, LTV) y distribuciones. Sector {sector} en la BMV."*). Strip markdown (`#`, `|`, `*`, `>`), collapse whitespace, clamp to ~155 chars on a word boundary.
- **Verify:** descriptions contain no `|`, `#`, or trailing `...`, and read as a sentence.

### C2. Fix the corrupted `??` emoji (S)
The emoji leading each FIBRA description markdown is serialized as two literal `0x3F` bytes — an encoding-loss bug (string written/read with a non-UTF-8 encoding somewhere in the pipeline).
- **Fix:** ensure UTF-8 end-to-end when reading/writing the description source; OR simply strip the leading emoji from the source content. Resolved automatically if C1 templates a clean description.
- **Verify:** no `??` in served HTML for any `/fibras/*` (raw bytes), nor in rendered content.

---

## 🟠 High — fix within 1 week

### H1. Return real 404 for unknown routes (M)
Unknown URLs (`/esta-pagina-no-existe`) return **HTTP 200** + empty title (soft 404). Risk: index bloat, Search Console "soft 404" reports.
- **Fix:** in the SPA fallback middleware, validate the route. For unknown FIBRA tickers / news slugs / paths not in the route table, respond `404` (and render the client NotFound view). Keep valid SPA routes at 200.
- **Verify:** `curl -I` an invalid URL returns `404`; valid routes still `200`.

### H2. Remove the duplicate `<h1>` on FIBRA profiles (S)
Two `level=1` headings render: the ficha title and the markdown description's leading `#`. 
- **Fix:** render markdown-sourced headings starting at `<h2>`/`<h3>` (offset markdown heading levels by +1), so the ficha title is the only `<h1>`.
- **Verify:** exactly one `<h1>` per FIBRA page.

### H3. Resolve the robots.txt AI-crawler conflict (S)
The Cloudflare-managed block `Disallow`s `ClaudeBot`/`GPTBot`/`Google-Extended`/etc., then a custom block re-`Allow`s them — plus a duplicate `User-agent: *` group. Ambiguous; may cause AI crawlers to honor the disallow.
- **Fix:** consolidate to a single coherent policy. One `User-agent: *` group; one explicit group per AI bot you intend to allow; remove the contradictory managed entries (disable the Cloudflare managed robots feature or override it).
- **Verify:** Google robots tester + OpenAI/Anthropic bot docs confirm `/` is allowed for intended agents.

---

## 🟡 Medium — fix within 1 month

### M1. Reduce CLS on data-heavy pages (M)
Detail-page CLS = 0.195 (target <0.10) from charts/tables/ticker reflow.
- **Fix:** reserve space — explicit `min-height`/`aspect-ratio` on chart containers; skeletons sized to final content; size the price-ticker row; ensure font-swap doesn't shift headings (`size-adjust`/fallback metrics).
- **Verify:** Lighthouse CLS <0.10 on `/fibras/*`.

### M2. Add JSON-LD to listing/tool pages (M)
`/noticias`, `/comparar`, `/fundamentales`, `/calendario`, `/calculadora` have no structured data.
- `/noticias` → `CollectionPage` + `ItemList` of `NewsArticle`.
- `/calculadora`, `/comparar` → `WebApplication`/`SoftwareApplication`.
- All → `BreadcrumbList`.

### M3. Fix accessibility contrast + ARIA (M)
43 contrast failures (muted/`text-xs` text) and invalid `aria-expanded` on `<tr>`.
- **Fix:** raise muted-foreground tokens to ≥4.5:1; move the expand affordance to a `<button>` or valid disclosure pattern.

### M4. Proxy external news OG images (M)
News `og:image` points to `lh3.googleusercontent.com` (expiry/breakage risk).
- **Fix:** cache/proxy thumbnails on your domain (ties in with dynamic OG, story 12-9).

### M5. Add author/expertise signals for YMYL (M)
No bylines/author entities on financial analysis.
- **Fix:** add a methodology-authorship / analyst bio, and `author`/`Person` or `publisher` expertise signals in schema. Strengthens E-E-A-T trust for financial content.

---

## 🟢 Low — backlog

- **L1.** Add `Content-Security-Policy` header.
- **L2.** Suppress `X-Powered-By: ASP.NET`.
- **L3.** Add `<lastmod>` to core sitemap entries.
- **L4.** Align server vs client `<title>` format (`usePageTitle`).
- **L5.** `FAQPage` schema (story 12-2) — rich-result upside.
- **L6.** `Dataset` schema for fundamentals tables (AI-citation friendly).
- **L7.** Add supporting indexable copy to `/comparar`.
- **L8.** Expand `sameAs` / build brand citations for entity authority.

---

## Roadmap

| Week | Focus | Items |
|------|-------|-------|
| 1 | SERP quality + indexing | C1, C2, H1, H2, H3 |
| 2–4 | CWV + schema + a11y | M1, M2, M3, M4, M5 |
| Backlog | Hardening + authority | L1–L8 |

**Note:** Several items align with in-flight Epic 12 stories (12-2 FAQPage, 12-3 structured financial data, 12-4 E-E-A-T/YMYL, 12-5 JSON-LD comparador/fundamentales/breadcrumbs, 12-9 dynamic OG images) — fold these findings into those stories where they overlap.
