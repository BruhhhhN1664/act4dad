# Git history

Continue the Lab 3 repository so earlier history is preserved (`https://github.com/BruhhhhN1664/Act3`).

```bash
git checkout -b lab4-postgresql
./setup.sh
```

Suggested commits (adjust to what you actually did — the history must be honest):

1. `Lab4: switch EF Core provider to Npgsql, secret-free configuration`
2. `Lab4: DbContext factory + unit of work, error classifier`
3. `Lab4: entity configurations with unique/filtered indexes and CHECK constraints`
4. `Lab4: InitialPostgres migration and seed-if-empty`
5. `Lab4: student registration, editing, eligibility (UC-12–14)`
6. `Lab4: equipment registration and editing (UC-15–16)`
7. `Lab4: multi-item atomic checkout (UC-06)`
8. `Lab4: histories and search/filter/sort (UC-07–09)`
9. `Lab4: integration tests`
10. `Lab4: README, use-case documents, traceability and evidence`

Export for submission (inspectable history without the repo):

```bash
git log --date=short --pretty=format:"%h %ad %an %s" --stat > docs/git-history.txt
git bundle create lab4-history.bundle --all
```
