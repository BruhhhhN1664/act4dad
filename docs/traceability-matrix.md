# Traceability matrix — use case → rules → implementation → verification

Verification ids refer to `verification-evidence.md` (fill the Observed/Evidence columns there). “Test” = method in `tests/EquipmentBorrowing.Tests/WorkflowTests.cs`.

| UC | Acceptance | Rubric | Rules | View / ViewModel | Service method | Repository / DB protection | Automated test | Manual evidence |
|---|---|---|---|---|---|---|---|---|
| 01 | A01 | R1 | 05–07,10,18,19 | CatalogView / CatalogViewModel | `EquipmentService.SearchAsync` | `EquipmentRepository.SearchAsync` | – | E-A01 |
| 02 | A02 | R1 | 10,18,19 | CatalogView | `EquipmentService.SearchAsync(availableOnly)` | same | – | E-A02 |
| 03 | A03 | R1,R2 | 08–11,14–16,18,19 | BorrowView / BorrowViewModel.BorrowOne | `BorrowingService.BorrowAsync` | row locks; `UX_Borrowings_ActiveByEquipment`; CHECK due>borrowed; FKs | `Single_borrow…`, `Expected_return…`, `Unavailable_item…`, `Restricted_student…`, `Limit_of_three…` | E-A03 |
| 04 | A04 | R1 | 14,18,19 | ActiveBorrowingsView | `GetActiveAsync` | `BorrowingRepository.SearchAsync` | `History_search_filter…` | E-A04 |
| 05 | A05 | R1,R2 | 10,13–16,18,19 | ActiveBorrowingsViewModel.ReturnSelected | `ReturnAsync` | row lock on borrowing; CHECK StatusMatchesReturn | `Return_records_the_date…` | E-A05 |
| 06 | A06 | R3 | 08–12,14–16,18,19 | BorrowViewModel.CheckoutMultiple | `CheckoutMultipleAsync` (one transaction) | `GetForUpdateAsync` locks; unique filtered index | `Multi_checkout_needs_two…`, `Multi_checkout_is_all_or_nothing…`, `Limit_of_three…` | E-A06 |
| 07 | A07 | R6,R7 | 14,18,19 | HistoryView / HistoryViewModel | `GetStudentHistoryAsync` | `BorrowingRepository.SearchAsync(StudentId)` | `History_search_filter…` | E-A07 |
| 08 | A08 | R6,R7 | 14,18,19 | HistoryView | `GetEquipmentHistoryAsync` | `SearchAsync(EquipmentId)` | – | E-A08 |
| 09 | A09 | R6,R7 | 17–19 | SearchView / SearchBorrowingsViewModel | `SearchAsync(BorrowingQuery)` | ILIKE + ordering + Id tie-break | `History_search_filter…` | E-A09 |
| 12 | A12 | R4,R2 | 01–02,16,18,19 | StudentsView / StudentsViewModel.Register | `StudentService.RegisterAsync` | `UX_Students_StudentNumberNormalized`; CHECK not blank | `Duplicate_student_number…`, `Blank_required_fields…` | E-A12 |
| 13 | A13 | R4 | 01–03,14,16,18,19 | StudentsViewModel.Save | `StudentService.UpdateAsync` | same unique index; FK Restrict | `Editing_keeps_history…` | E-A13 |
| 14 | A14 | R4 | 04,08,13,14,18,19 | StudentsViewModel.Permit/Restrict | `SetEligibilityAsync` | – | `Restricted_student…`, `Repeating_an_eligibility…` | E-A14 |
| 15 | A15 | R5,R2 | 05–06,10,16,18,19 | EquipmentView / EquipmentManagementViewModel | `EquipmentService.RegisterAsync` | `UX_Equipment_IdentifierNormalized` | `Duplicate_equipment_identifier…` | E-A15 |
| 16 | A16 | R5 | 05–07,10,14,16,18,19 | EquipmentManagementViewModel.Save | `EquipmentService.UpdateAsync` | same | `Editing_keeps_history…` | E-A16 |
| – | A17 | R2 | 01,05,10,14,16 | – | – | migration `InitialPostgres`; constraints | `Database_itself_rejects_a_second_active…` | E-A17 |
| – | A18 | R3,R7,R8 | 15,18,19 | MainWindowViewModel (Retry), status bars | `ServiceBase.GuardAsync`, `CommitAsync` | `NpgsqlErrorClassifier` | `Multi_checkout_is_all_or_nothing…` | E-A18 |
| – | A19 | R8 | all | – | – | – | – | E-A19 |
