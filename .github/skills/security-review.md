---
name: security-review
description: 'AI-powered codebase security scanner that reasons about code like a security researcher — tracing data flows, understanding component interactions, and catching vulnerabilities that pattern-matching tools miss. Use this skill when asked to scan code for security vulnerabilities, find bugs, check for SQL injection, XSS, command injection, exposed API keys, hardcoded secrets, insecure dependencies, access control issues, or any request like "is my code secure?", "review for security issues", "audit this codebase", or "check for vulnerabilities".'
---

# Security Review

An AI-powered security scanner that reasons about your codebase the way a human security
researcher would — tracing data flows, understanding component interactions, and catching
vulnerabilities that pattern-matching tools miss.

## Execution Workflow

Follow these steps **in order** every time:

### Step 1 — Scope Resolution
Determine what to scan:
- If a path was provided, scan only that scope
- If no path given, scan the **entire project** starting from the root
- Identify the language(s) and framework(s) in use

### Step 2 — Dependency Audit
Audit dependencies first:
- Check `.csproj` / `Directory.Packages.props` for NuGet packages with known CVEs
- Flag packages with known CVEs, deprecated crypto libs, or suspiciously old pinned versions

### Step 3 — Secrets & Exposure Scan
Scan ALL files (including config, env, CI/CD, Dockerfiles, IaC) for:
- Hardcoded API keys, tokens, passwords, private keys
- `.env` files accidentally committed
- Secrets in comments or debug logs
- Cloud credentials (AWS, GCP, Azure, etc.)
- Database connection strings with credentials embedded

### Step 4 — Vulnerability Deep Scan

**Injection Flaws**
- SQL Injection: raw queries with string interpolation, ORM misuse, second-order SQLi
- Command Injection: exec/spawn/system with user input
- Header, Log injection

**Authentication & Access Control**
- Missing authentication on sensitive endpoints
- Broken object-level authorization (BOLA/IDOR)
- JWT weaknesses (alg:none, weak secrets, no expiry validation)
- Session fixation, missing CSRF protection
- Privilege escalation paths

**Data Handling**
- Sensitive data in logs, error messages, or API responses
- Missing encryption at rest or in transit
- Insecure deserialization
- Path traversal / directory traversal
- SSRF (Server-Side Request Forgery)

**Cryptography**
- Use of MD5, SHA1, DES for security purposes
- Hardcoded IVs or salts
- Weak random number generation
- Missing TLS certificate validation

**Business Logic**
- Race conditions (TOCTOU)
- Integer overflow in financial calculations
- Missing rate limiting on sensitive endpoints
- Predictable resource identifiers

### Step 5 — Cross-File Data Flow Analysis
- Trace user-controlled input from entry points all the way to sinks
- Identify vulnerabilities that only appear when looking at multiple files together

### Step 6 — Self-Verification Pass
For EACH finding:
1. Re-read the relevant code with fresh eyes
2. Ask: "Is this actually exploitable, or is there sanitization I missed?"
3. Assign final severity: CRITICAL / HIGH / MEDIUM / LOW / INFO

### Step 7 — Generate Security Report

### Step 8 — Propose Patches
For every CRITICAL and HIGH finding, generate a concrete patch showing before/after.

Explicitly state: **"Review each patch before applying. Nothing has been changed yet."**

## Severity Guide

| Severity | Meaning | Example |
|----------|---------|---------|
| 🔴 CRITICAL | Immediate exploitation risk | SQLi, RCE, auth bypass |
| 🟠 HIGH | Serious vulnerability | XSS, IDOR, hardcoded secrets |
| 🟡 MEDIUM | Exploitable with conditions | CSRF, open redirect, weak crypto |
| 🔵 LOW | Best practice violation | Verbose errors, missing headers |
| ⚪ INFO | Observation worth noting | Outdated dependency (no CVE) |

## Output Rules

- **Always** produce a findings summary table first (counts by severity)
- **Never** auto-apply any patch — present patches for human review only
- **Always** include a confidence rating per finding (High / Medium / Low)
- **Group findings** by category, not by file
- **Be specific** — include file path, line number, and the exact vulnerable code snippet
