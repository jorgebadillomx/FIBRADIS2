# FIBRADIS — Full Website SEO Audit

**Site:** https://fibrasinmobiliarias.com/
**Date:** 2026-06-13
**Crawl method:** Raw HTML fetch + rendered DOM (Chrome) + Lighthouse + performance trace
**Pages in sitemap:** 67 (11 core pages, 19 FIBRA profiles, 37 news articles)

---

## Executive Summary

**Overall SEO Health Score: 84 / 100** — *Strong*

**Business type detected:** Financial information / analysis platform (YMYL — *Your Money Your Life*). Niche: análisis de FIBRAs (REITs mexicanos) en la BMV. Single-language (es-MX), national (México). Not a local/brick-and-mortar business — no GBP/maps analysis applicable.

This is a well-architected SPA (React + ASP.NET) with **server-side meta injection per route**, a complete `llms.txt`, strong structured data, and excellent homepage performance. The foundation is above average for the niche. The issues that remain are concentrated and fixable — most notably a content-generation bug that pollutes the FIBRA-profile meta descriptions and a soft-404 problem.

### Top 5 Critical / High Issues
1. **FIBRA-profile meta description is a raw markdown dump** — descriptions read `" ?? Fibra Uno | FUNO11 Ticker: FUNO11 ... | Campo | Detalle | ..."`, truncated at 160 chars with table pipes. This feeds `<meta name="description">`, `twitter:description`, **and** the `FinancialProduct.description` schema field. (19 pages)
2. **Corrupted emoji `??`** — an emoji at the start of each FIBRA description markdown is being written as two literal `0x3F` bytes (encoding loss, not UTF-8). Visible in SERP-facing meta and in rendered page content.
3. **Soft 404** — unknown routes (e.g. `/esta-pagina-no-existe`) return **HTTP 200** with an empty `<title>` instead of `404`. Risk of index bloat and Search Console "soft 404" flags.
4. **Duplicate `<h1>` on FIBRA profiles** — the page renders two `level=1` headings: the ficha title *and* the markdown description's leading `# heading`. Breaks heading hierarchy.
5. **CLS 0.195 on data-heavy detail pages** ("needs improvement", target <0.10) — layout shifts from charts/tables/font-swap rendering after load.

### Top 5 Quick Wins
1. Strip markdown/table syntax and clamp FIBRA descriptions to a clean ~155-char summary (fixes #1 and the schema field at once).
2. Fix the emoji encoding (write UTF-8) or drop the leading emoji from the description source (fixes #2).
3. Demote the markdown description's leading heading from `<h1>` to `<h2>`/`<h3>` (fixes #4).
4. Reserve space for charts/tables (explicit min-height / aspect-ratio) to cut detail-page CLS.
5. Add JSON-LD to listing/tool pages that currently have none (`/noticias`, `/comparar`, `/fundamentales`, `/calendario`, `/calculadora`).

---

## Technical SEO — Score 85/100

| Check | Result |
|-------|--------|
| HTTPS | ✅ Enforced; `http://` → `https://` **301** |
| www handling | ✅ `www.` → apex **301** |
| robots.txt | ✅ Present, with `Sitemap:` directive |
| Sitemap | ✅ Valid XML, 67 URLs, correct `application/xml` |
| Canonical tags | ✅ Self-referential, absolute, per-route |
| HSTS | ✅ `max-age=31536000; includeSubDomains` |
| X-Frame-Options | ✅ `DENY` |
| X-Content-Type-Options | ✅ `nosniff` |
| Referrer-Policy | ✅ `strict-origin-when-cross-origin` |
| Permissions-Policy | ✅ camera/mic/geo locked down |
| Content-Security-Policy | ⚠️ **Missing** |
| X-Powered-By | ⚠️ Leaks `ASP.NET` (minor info disclosure) |
| 404 handling | ❌ **Soft 404** — unknown routes return 200 |
| CDN | ✅ Cloudflare (HTTP/3, edge caching) |

**robots.txt conflict (High for AI crawlers):** the file contains a Cloudflare-managed block that `Disallow`s `ClaudeBot`, `GPTBot`, `Google-Extended`, `Applebot-Extended` (etc.), followed by a *second* custom block that re-`Allow`s those same agents, plus a **second `User-agent: *` group**. Multiple groups for the same agent is ambiguous per spec and depends on crawler merge behavior — the intent (allow AI training/grounding) is undermined by the managed block above it. Consolidate into a single coherent set of groups.

**Sitemap notes:** core pages (`/`, `/fibras`, `/comparar`…) have no `<lastmod>`; only FIBRA profiles and news do. Low priority (Google largely ignores `lastmod` reliability), but adding it is cheap. No `<changefreq>`/`<priority>` — fine.

---

## Content Quality — Score 88/100 (strongest area)

- **Depth & uniqueness:** FIBRA profiles are genuinely rich — market data, fundamentals (Cap Rate, NAV/CBFI, LTV, NOI/FFO margin) **with per-page source citations** ("Reportado en página 13…"), an analyst summary, operational/financial signals, risk alerts, distribution history, and a long-form company description. This is high-value, hard-to-replicate content.
- **E-E-A-T (critical for YMYL):**
  - ✅ Clear disclaimer in footer: *"…solo con fines informativos y educativos. No constituye asesoría financiera… FIBRADIS no está regulado por la CNBV."*
  - ✅ Methodology / "Acerca" page documenting data sources and fundamentals calculation.
  - ✅ Data provenance cited inline (page numbers of source reports).
  - ⚠️ **No visible author/analyst attribution or bylines.** For YMYL financial content, named expertise (or an "About the team / methodology authorship") strengthens trust signals.
- **Thin content:** none observed on profiles. Tool pages (`/comparar`, `/calculadora`) are interactive and naturally light on indexable text — acceptable, but `/comparar` would benefit from supporting copy.
- **Defect:** the markdown-dump meta description (see Critical #1) undercuts otherwise excellent content at the SERP-snippet level.

---

## On-Page SEO — Score 78/100

| Element | Status |
|---------|--------|
| Title tags | ✅ Unique per route, ~50–70 chars, brand-suffixed |
| Meta descriptions (core/news pages) | ✅ Well-written, compelling, correct length |
| Meta descriptions (FIBRA profiles) | ❌ Markdown dump + `??` (Critical #1/#2) |
| Canonical | ✅ Correct |
| OpenGraph | ✅ Complete (title, desc, type, url, image, locale, dimensions, alt) |
| Twitter Card | ✅ `summary_large_image`, `@fibradis` |
| `og:type` | ✅ `article` on news, `website` elsewhere |
| H1 | ❌ **Two H1s** on FIBRA profiles |
| Heading hierarchy | ⚠️ H1→H2→H3 otherwise consistent |
| Internal linking | ✅ Strong — global nav, in-ficha section nav, breadcrumbs, related news, cross-FIBRA ticker bar |
| `lang` attribute | ✅ `es-MX` |
| hreflang | ➖ N/A (single language); consider `x-default` only if EN added |

**Title inconsistency:** the server sends `Fibra Uno (FUNO11) | FIBRADIS — Fibras Inmobiliarias`, but the client (`usePageTitle`) rewrites the rendered title to `FUNO11 — Fibra Uno | Fibras Inmobiliarias`. Google uses the rendered value; harmless but worth aligning the two formats for consistency.

---

## Schema / Structured Data — Score 85/100

**Implemented (valid JSON-LD `@graph`):**
- Homepage: `Organization` + `WebSite` + `FinancialService`
- FIBRA profile: `FinancialProduct` + `Organization` + `BreadcrumbList`
- News article: `NewsArticle` + `Organization` + `ImageObject` (`og:type=article`)
- Guide (`/conoce-las-fibras`): `Article`

**Gaps / opportunities:**
- ❌ No JSON-LD on `/comparar`, `/noticias`, `/fundamentales`, `/calendario`, `/calculadora`. Add `CollectionPage`/`ItemList` (noticias), `WebApplication`/`SoftwareApplication` (calculadora/comparar), `BreadcrumbList` (all).
- ⚠️ `FinancialProduct.description` inherits the broken markdown-dump text (Critical #1).
- 💡 `FAQPage` not yet present (tracked in story 12-2) — high rich-result upside for the guide and FIBRA pages.
- 💡 Consider `Dataset` schema for the fundamentals tables (strong fit for a financial-data site, AI-citation friendly).

---

## Performance (Core Web Vitals) — Score 82/100

*Lab trace, Chrome, unthrottled network/CPU — real mobile will be higher.*

| Metric | Homepage | FIBRA profile | Target |
|--------|----------|---------------|--------|
| LCP | **355 ms** ✅ | — | <2500 ms |
| TTFB | 67 ms ✅ | — | <800 ms |
| CLS | **0.08** ✅ | **0.195** ❌ | <0.10 |
| LCP render delay | 288 ms | — | minimize |

- ✅ Excellent loading architecture: GTM deferred to idle, fonts non-blocking (`preload`+swap), `modulepreload` for vendor chunks, code-split bundles (react/query/charts), Cloudflare edge.
- ❌ **CLS 0.195 on data-heavy pages** — charts, fundamentals tables, and the price ticker render/reflow after data loads. Reserve layout space (min-height / aspect-ratio / skeletons sized to final content).
- ➖ **No CrUX field data** for these URLs — the site has insufficient real-user traffic to populate Chrome UX Report. This is itself a signal: rankings/traffic are still ramping. Re-check field data once traffic grows.

---

## Images — Score 80/100

- ✅ Global OG image present with explicit `1200×630` + `og:image:alt`.
- ✅ Favicon/logo images carry `alt` (ticker); charts are inline SVG (no raster weight).
- ⚠️ **Single static `og-image.png` site-wide** — FIBRA profiles and news share the generic OG image. Per-entity dynamic OG images (tracked in story 12-9) would lift social/AI-preview CTR.
- ⚠️ **News `og:image` points to external Google-hosted URLs** (`lh3.googleusercontent.com/...`) — these can expire or hotlink-break. Proxy/cache them on your own domain.

---

## AI Search Readiness (GEO) — Score 85/100

- ✅ **`llms.txt` is excellent** — structured overview, principal pages with descriptions, full table of the 19 covered FIBRAs, plain-language explanation of FIBRAs, independence/disclaimer note, contact. This is a model implementation.
- ✅ Content is highly **citable**: short factual passages, labeled metrics, sourced figures, tables — ideal for AI Overviews / ChatGPT / Perplexity extraction.
- ✅ Robots re-allows `GPTBot`, `ClaudeBot`, `Google-Extended` (intent: permit AI grounding/training).
- ⚠️ **robots.txt ambiguity** (see Technical) risks AI crawlers honoring the *managed* `Disallow` block and never reaching the re-allow — verify with each bot's tester.
- ⚠️ **Brand/authority signals thin** — `sameAs` lists Twitter + LinkedIn only; no author entities. Building citations/mentions and named expertise would strengthen entity authority.

---

## Category Score Summary

| Category | Weight | Score | Weighted |
|----------|--------|-------|----------|
| Technical SEO | 22% | 85 | 18.7 |
| Content Quality | 23% | 88 | 20.2 |
| On-Page SEO | 20% | 78 | 15.6 |
| Schema / Structured Data | 10% | 85 | 8.5 |
| Performance (CWV) | 10% | 82 | 8.2 |
| AI Search Readiness | 10% | 85 | 8.5 |
| Images | 5% | 80 | 4.0 |
| **TOTAL** | **100%** | | **≈ 84** |

---

## Accessibility (Lighthouse, mobile) — 93/100

Not a scored SEO category but affects UX signals:
- ❌ **43 elements fail color contrast** — mostly muted text (`opacity-60`), small badges (`text-xs`), and table "diferencia"/"nota" cells. Bump muted-foreground tokens to meet 4.5:1.
- ❌ `aria-expanded` set on `<tr>` rows (invalid for that role) in the distributions table — move to a button or use a valid pattern.
- Lighthouse Best Practices: **100**, SEO: **100**.
