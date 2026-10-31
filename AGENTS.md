# AGENTS.md — TUM Trust & Security Engineering Harness

This repository is governed by the **ECC (Agent Harness OS)** engineering standards adapted for the **Tum.Trust.Security** identity verification ecosystem. All AI agents (Antigravity, Claude Code, Codex, etc.) must adhere to these guidelines.

---

## 1. Project Context & Architecture

- **Project**: `Tum.Trust.Security`
- **Purpose**: Enterprise-grade Sovereign Identity Verification SDK & Hardware Security Platform
- **Target Runtimes**: Multi-targeted for `.NET 5.0`, `.NET 8.0`, and `.NET 10.0`
- **Key Modules**:
  - `Tum.Trust.Security`: Core SDK, PKCE, OIDC, DOPA, ThaID, ICAO PKD, Biometrics, Hardware Kiosks, Sovereign Trust Anchors
  - `Tum.Log`: High-performance tamper-evident enterprise audit logging engine
  - `Tests`: Comprehensive xUnit test suite (`Tum.Trust.Security.Tests`)
  - `MockServer`: Standalone local mock server for testing identity providers without live credentials

---

## 2. Core Engineering Principles

1. **Plan First**: For changes spanning 2+ files or complex logic, plan before touching code.
2. **Test-First (TDD)**: Prefer writing a failing test reproducing a bug or defining new behavior before implementation.
3. **Fail Closed**: In security and identity boundaries, never assume validity. Fail loud and securely if evidence is missing or invalid.
4. **Immutability & Modern C#**: Use C# `record`, `init`-only properties, nullable reference types (`#nullable enable`), and cancellation tokens (`CancellationToken`) across all async operations.
5. **No Secret Leakage**: Zero hardcoded secrets, test API keys, or private tokens. All credentials must come from secure configuration or environment variables.
6. **Strict Verification**: Every change must pass the 6-phase verification loop before declaring done.

---

## 3. Discovered Skills Catalog

The repository provides the following specialized skills in `.agents/skills/` and `.claude/skills/`:

| Skill | Trigger / When to Use |
| :--- | :--- |
| `dotnet-patterns` | Writing or refactoring C# code, service contracts, DI lifecycles, async/await, or error handling. |
| `csharp-testing` | Creating or reviewing xUnit tests, mocks, test fixtures, and assertions. |
| `security-review` | Implementing authentication, token parsing, PKCE, cryptography, secrets, or input validation. |
| `agent-security-hardening` | Reviewing agent runtime boundaries, tool execution safety, and anti-tamper mechanisms. |
| `api-design` | Designing SDK public API methods, RESTful endpoints, error models (`ProblemDetails`), and versioning. |
| `api-connector-builder` | Building a new identity or trust provider connector following repository patterns. |
| `architecture-decision-records` | Recording significant architectural, security, or cryptographic decisions in `docs/adr/`. |
| `verification-loop` | Pre-commit/pre-PR 6-phase check (build, type-check, format, test, security scan, diff review). |
| `tdd-workflow` | Red-Green-Refactor development cycle with >=80% test coverage target. |
| `agent-self-evaluation` | Self-rating output on 5 dimensions (Accuracy, Completeness, Clarity, Actionability, Conciseness) upon task completion. |
| `conventional-commit-review` | Reviewing staged diffs and generating standard Conventional Commits messages. |
| `safety-guard` | Protecting against destructive terminal operations (`rm -rf`, `git push --force`, DROP TABLE). |
| `add-identity-provider` | In-depth repository guide for integrating new identity providers into Tum.Trust.Security. |
| `integrate-tum-trust` | Host application integration guide for consuming Tum.Trust.Security in external systems. |

---

## 4. Verification & Quality Gates

Before declaring any coding task complete:
```powershell
# 1. Build Verification
dotnet build --configuration Release

# 2. Strict Analyzers / Lint
dotnet format --verify-no-changes

# 3. Test Suite
dotnet test --no-build --verbosity normal

# 4. Security Audit
git diff --staged
# Verify no keys, secrets, or sensitive logs are staged
```

---

## 5. Commit Standards

Use Conventional Commits:
- `feat(scope)`: New SDK capability or provider
- `fix(scope)`: Bug fix or edge-case handling
- `security(scope)`: Vulnerability fix, crypto update, or auth hardening
- `test(scope)`: Adding or improving tests
- `refactor(scope)`: Code restructuring without functional change
- `docs(scope)`: Documentation, architecture updates, or ADRs
