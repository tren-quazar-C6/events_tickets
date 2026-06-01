---
name: commit-messages
description: Format commit messages using conventional commits (feat/chore/docs) with no signing, title + short body in single message
---

## Commit Message Format

Follow conventional commits format for all commits. Structure:
- **Type**: `feat` (new feature), `chore` (maintenance, refactoring, deps), or `docs` (documentation)
- **Title**: One line, 50 chars max, imperative mood (e.g., "add email validation", not "added email validation")
- **Body**: Optional short description (2-3 lines max) explaining the "why" if non-obvious
- **No signing**: Commits are unsigned (no `-S` flag, no GPG)

## Examples

**Feature:**
```
feat: add email confirmation flow for user registration

Sends confirmation link via SMTP when new users register.
Implements timeout and retry logic for failed sends.
```

**Chore:**
```
chore: refactor IEmailService interface

Consolidate email config fallback patterns into single source of truth.
```

**Documentation:**
```
docs: add troubleshooting guide for MongoDB audit logs

Explains how to verify audit events during testing.
```

## When Committing

Use `git commit -m "message"` with title and body in one `-m` flag, separated by blank line:
```bash
git commit -m "feat: add correlation ID header support

Enables request tracing across audit logs."

git commit -m "chore: update Dapper ORM dependency"
```

Never include "Co-Authored-By" footers unless explicitly told to by the user.
No signing — commits are unsigned.
