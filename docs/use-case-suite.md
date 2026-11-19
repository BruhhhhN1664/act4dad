# Use-case suite — fourteen required cases (adapted to this implementation)

> The instructor's separate *Use Case Suite* was not available while this file was prepared. These tables are written from
> Appendix A/B/C of the lab sheet. **Merge them into your supplied suite** (keep its required fields) and correct any wording.

Common: **Primary actor** Custodian · **System** Campus Equipment Borrowing System · all lists load asynchronously; failures show a
message without credentials (BR-18/19).

---
### UC-01 View Equipment Catalog
| Field | Content |
|---|---|
| Precondition | Database reachable |
| Trigger | Custodian opens the **Catalog** tab (or presses Search/Clear) |
| Main flow | 1 Custodian opens tab → 2 system loads all equipment asynchronously → 3 shows identifier, name, description, availability |
| Alternates | A1 No equipment: explicit empty-state message. A2 Search text with no match: “no equipment matches”. A3 Database unreachable: connection-error message, not an empty list |
| Rules | BR-05–07, 10, 18, 19 |
| Postcondition / acceptance | No data changes; list equals the `Equipment` table (A01) |

### UC-02 Find Available Equipment
| Field | Content |
|---|---|
| Trigger | Custodian ticks **Show only available equipment** (optionally types text) |
| Main flow | System lists only items with `IsAvailable = true` |
| Alternates | A1 none available / no match → empty-state; A2 failed load → error |
| Rules | BR-10, 18, 19 |
| Acceptance | No changes; borrowed items are excluded (A02) |

### UC-03 Borrow Equipment
| Field | Content |
|---|---|
| Precondition | Student and item exist |
| Main flow | 1 Select student → 2 tick **one** available item → 3 enter expected return date → 4 *Borrow one item* → 5 system revalidates inside a transaction → 6 creates one active borrowing, marks item unavailable, commits → 7 success message, lists refresh |
| Rejections (nothing is saved) | student not allowed · student/item no longer exists · item unavailable · student already at limit (3) · date missing or not later than today |
| Rules | BR-08–11, 14–16, 18, 19 |
| Acceptance | One active row + unavailable item; survives restart (A03) |

### UC-04 View Active Borrowings
| Field | Content |
|---|---|
| Main flow | Opens **Active / Return**; system lists `Status = Active` with student and equipment, soonest due first |
| Alternates | A1 none active: empty-state; A2 connection error: error message |
| Rules | BR-14, 18, 19 · Acceptance: returned records excluded (A04) |

### UC-05 Return Equipment
| Field | Content |
|---|---|
| Main flow | Select an active borrowing → *Return selected item* → system locks the record, checks it is still active and date ≥ checkout → marks it returned with today's local date, makes item available, commits |
| Rejections | already returned (no change) · record/item missing (stale) · return date before checkout date |
| Rules | BR-10, 13–16, 18, 19 · Acceptance: same row becomes Returned, history kept (A05) |

### UC-06 Check Out Multiple Equipment Items
| Field | Content |
|---|---|
| Main flow | Select student → tick two or more items → expected return date → *Check out ticked items* → **one transaction**: lock student, lock items, revalidate everything, write one active borrowing per item, mark each item unavailable, commit |
| Rejections (zero changes) | fewer than two items · duplicate item · item unavailable/missing · date invalid · student not allowed · would exceed 3 active (exactly 3 is allowed) |
| Failure | Any exception after a write was attempted → rollback, error shown, no partial rows; connection lost during commit → “outcome unknown” (BR-18) |
| Rules | BR-08–12, 14–16, 18, 19 · Acceptance: A06 |

### UC-07 View Student Borrowing History
| Field | Content |
|---|---|
| Main flow | History tab → select a student → asynchronous load of every active and returned borrowing, newest first |
| Alternates | A1 student has none: “has never borrowed”; A2 student deleted/stale: not-found message; A3 connection error: error text (distinct from empty) |
| Rules | BR-14, 18, 19 · Acceptance: A07 |

### UC-08 View Equipment Borrowing History
Same as UC-07 for an equipment item (student, dates, status). Rules BR-14, 18, 19 · Acceptance A08.

### UC-09 Search, Filter, and Sort Borrowings
| Field | Content |
|---|---|
| Main flow | Enter text (student name, student number or equipment name; trimmed, case-insensitive, partial) · choose status All/Active/Returned · choose sort field (checkout date / expected return date) and direction · Search; Clear resets |
| Ordering | Chosen field and direction, then borrowing Id ascending as tie-breaker |
| Alternates | no match → explicit empty state; connection error → error message |
| Rules | BR-17–19 · Acceptance: A09 |

### UC-12 Register Student
| Field | Content |
|---|---|
| Main flow | Students tab → *New* → enter number, name, optional contact → *Register new* → system trims, normalizes, checks uniqueness, saves; new student is eligible |
| Rejections | blank number/name · duplicate number ignoring case/space · field too long |
| Rules | BR-01–02, 16, 18, 19 · Acceptance: A12 |

### UC-13 Update Student Information
| Field | Content |
|---|---|
| Main flow | Find and select student → edit number/name/contact → *Save changes*; blank contact clears it |
| Rejections | blank required fields · number used by another student · student no longer exists |
| Postcondition | Same Id; borrowings and eligibility unchanged |
| Rules | BR-01–03, 14, 16, 18, 19 · Acceptance: A13 |

### UC-14 Change Student Borrowing Eligibility
| Field | Content |
|---|---|
| Main flow | Select student → *Allow borrowing* or *Stop borrowing* |
| Effects | Restricted: new issues rejected; existing active borrowings stay active and can be returned |
| Alternate | Repeating the current status: “already …; no change was made” |
| Rules | BR-04, 08, 13, 14, 18, 19 · Acceptance: A14 |

### UC-15 Register Equipment
| Field | Content |
|---|---|
| Main flow | Equipment tab → identifier, name, optional description → *Register new*; new item is available with no borrowing |
| Rejections | blank identifier/name · duplicate identifier ignoring case/space |
| Rules | BR-05–06, 10, 16, 18, 19 · Acceptance: A15 |

### UC-16 Update Equipment Information
| Field | Content |
|---|---|
| Main flow | Select item → edit identifier/name/description → *Save changes*; blank description clears it |
| Protected | Availability, borrowing status, return dates are not editable fields (no UI, service ignores them) |
| Rejections | blank required values · identifier of another item · item no longer exists |
| Rules | BR-05–07, 10, 14, 16, 18, 19 · Acceptance: A16 |
