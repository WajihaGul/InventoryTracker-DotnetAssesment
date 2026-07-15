# AI Notes

## Tools Used

I used Claude (Anthropic) as a pairing partner — for scaffolding boilerplate and stress-testing design decisions before committing to them. I reviewed every piece against what I actually know before accepting it, and overrode it where my own judgment said otherwise — for example, choosing **SQL Server LocalDB** for the application database over an initially-suggested SQLite, since it's the engine I already work with daily. (SQLite does appear once — but only inside the unit test project, as a disposable in-memory database. The application itself never touches it.)

## Something It Got Wrong

The first version of the unit tests for `RecordMovementAsync` ran against EF Core's In-Memory provider, and every one of them failed with a `TransactionIgnoredWarning` — that provider doesn't support transactions at all.

This wasn't a small bug. `RecordMovementAsync`'s correctness guarantee comes entirely from a `Serializable` transaction preventing two concurrent "Out" requests from both succeeding on stale stock. Testing it against a provider that can't run a transaction would have let the tests pass green while proving nothing about the one rule the assignment cares most about. I rebuilt those four tests against a real in-memory SQLite connection instead, which handles transactions properly, and left the three read-only tests on the In-Memory provider since they don't need that guarantee.

## Other Calls I Made

- Kept the derived-stock design uncompromising — no `CurrentStock` column or cached shortcut anywhere, even where it would have quietly made something simpler to write.
- Shipped one stretch goal done properly instead of three done halfway, per the assignment's own guidance.
- Didn't stop at green tests — I drove the zero-floor rule through the actual UI against LocalDB myself, since a passing test proves the logic, not that the transaction holds on a real SQL engine.