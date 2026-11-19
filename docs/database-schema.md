# PostgreSQL schema (as produced by migration `InitialPostgres`)

```mermaid
erDiagram
    Students ||--o{ Borrowings : "StudentId (RESTRICT)"
    Equipment ||--o{ Borrowings : "EquipmentId (RESTRICT)"
    Students {
        int Id PK
        varchar StudentNumber
        varchar StudentNumberNormalized UK
        varchar FullName
        varchar Contact "optional"
        bool IsAllowedToBorrow
    }
    Equipment {
        int Id PK
        varchar EquipmentIdentifier
        varchar EquipmentIdentifierNormalized UK
        varchar Name
        varchar Description "optional"
        bool IsAvailable
    }
    Borrowings {
        int Id PK
        int StudentId FK
        int EquipmentId FK
        date BorrowedAt
        date ExpectedReturnAt
        date ReturnedAt "null while active"
        varchar Status "Active | Returned"
    }
```

| Constraint | Purpose |
|---|---|
| `UX_Students_StudentNumberNormalized` (unique) | BR-01 |
| `UX_Equipment_IdentifierNormalized` (unique) | BR-05 |
| `UX_Borrowings_ActiveByEquipment` (unique on `EquipmentId` WHERE `ReturnedAt IS NULL`) | BR-10: one active borrowing per item |
| `CK_Borrowings_DueAfterBorrowed` | BR-11 |
| `CK_Borrowings_StatusMatchesReturn` | Active ⇔ no return date; Returned ⇒ date ≥ checkout (BR-13) |
| `CK_Students_*NotBlank`, `CK_Equipment_*NotBlank` | BR-02, BR-06 |
| FKs `Borrowings → Students/Equipment`, `ON DELETE RESTRICT` | BR-14 |
