# Design Write-up

## 1. Concurrency and Double-Sell Prevention

Seat availability is stored in SQL Server.

Reservation requests are processed inside a database transaction using serializable isolation. The requested seats are checked before they are changed from `available` to `confirmed`.

If another request has already confirmed one of the requested seats, the losing request returns a conflict response instead of creating another reservation.

This ensures that the same seat cannot be confirmed for multiple reservations.

## 2. Per-User Limit

Each show has a per-user seat limit.

The default limit is 4 seats per user.

Before confirming a reservation, the system checks the number of seats already confirmed by that user for the show and ensures that the new reservation does not exceed the configured limit.

## 3. Idempotency

Each reservation request contains an idempotency key.

The reservation stores:

- show ID
- user ID
- idempotency key
- request hash

When the same idempotency key is received again with the same request, the original reservation is returned.

If the same idempotency key is used with a different request, the API returns a conflict.

This prevents duplicate reservations when a client retries a request.

## 4. Partial Requests

Reservation requests are handled using an all-or-nothing approach.

If any requested seat is unavailable, the complete reservation request is rejected and none of the requested seats are confirmed.

This prevents partially successful reservations.

## 5. Money Handling

All prices are stored and calculated as integer paise.

Floating-point values are not used for reservation amounts.

For example:

```text
25000 paise = ₹250