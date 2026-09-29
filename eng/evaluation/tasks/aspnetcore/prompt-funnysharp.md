## Business brief: minimal API over a small shop

You are implementing a Minimal API surface over a small order/product domain, hosted and driven
by a fixed xUnit suite that uses Microsoft.AspNetCore.TestHost. The test suite and the neutral
domain contract are already provided in the build directory; you map the endpoints behind the
seam below and the tests must pass.

Seam (you must define this exactly):

public static class Api
{
    public static void MapApi(WebApplication app)
}

The tests build the application themselves (WebApplication.CreateBuilder(),
builder.WebHost.UseTestServer(), Build(), Api.MapApi(app), await app.StartAsync()) and call your
endpoints through the test server client; you only map endpoints and implement the rules behind
them.

Declare your types in the global namespace (no namespace declaration) in the files you write.
The provided contract defines the request and response DTOs (OrderRequest, OrderLine,
OrderCreated, PaymentRequest, PaymentAccepted), the domain records (Product, StoredOrder), and
the static in-memory ShopData the endpoints read and write; do not redefine them. Requests and
responses use the host's standard ASP.NET Core JSON options, so JSON property names are the
camelCase forms of the record properties (customerEmail, lines, sku, quantity, promoCode,
cardToken, orderId, paid, id, name, price). ShopData is deterministic:

- Products is the fixed catalog: keyboard "Mechanical keyboard" 40.00, mouse "Wireless mouse"
  15.00, monitor "27-inch monitor" 220.00.
- FindProduct(id) returns null when no product matches the id; FindOrder(id) returns null when
  no stored order matches the id.
- AddOrder(customerEmail, total) stores the order with Paid = false and returns its id; ids
  start at 1001 and increase by one per stored order.
- MarkOrderPaid(id) flips the stored order's Paid flag.
- RemoveOrder(id) removes the order and returns false for an unknown id.
- Charge(cardToken, amount) returns false and records nothing when cardToken equals
  DeclinedCardToken ("declined" after Reset); otherwise it records (cardToken, amount) in
  Charges and returns true.
- Reset() clears the stored orders and charges, resets the next order id to 1001, and
  DeclinedCardToken to "declined"; the tests call it before every test.

Business rules, by endpoint:

POST /orders - the request body is the OrderRequest JSON (customerEmail, lines, promoCode; each
line is sku and quantity). Run every independent validation check and report every failure, in
this order:

1. invalid-email: CustomerEmail does not contain '@', or contains no '.' after the '@'.
2. empty-order: Lines is empty.
3. invalid-quantity:<sku>: a line's Quantity is outside 1..99 (one error per offending line, in
   line order).
4. unknown-promo: PromoCode is neither null, empty, SAVE10, nor FREESHIP.

Any validation error produces 400 with an HttpValidationProblemDetails whose errors dictionary
holds every reported code, in rule order, under the single key "order"; the content type is
application/problem+json and no order is stored. Otherwise price the order: every line's sku
must be in ShopData.Products, otherwise unknown-sku:<sku> (one per missing line, in line order)
with the same 400 validation-problem response and no stored order. The total is the sum of
price x quantity over all lines plus 5.00 shipping; promo SAVE10 removes 10% of the goods
subtotal (shipping unaffected); promo FREESHIP removes the shipping; round the final total to
two decimals. On success the order is stored through ShopData.AddOrder and the response is 201
whose body is the OrderCreated JSON (orderId).

GET /products/{id} - a known product responds 200 with the Product JSON (id, name, price). An
unknown product responds 404 with a ProblemDetails whose detail is product-not-found:<id>.

DELETE /orders/{id} - an unknown order responds 409 with a ProblemDetails whose detail is
order-not-found:<id>. An already paid order responds 409 with a ProblemDetails whose detail is
order-paid and stays stored. Otherwise the order is removed through ShopData.RemoveOrder and
the response is 204.

POST /orders/{id}/pay - the request body is the PaymentRequest JSON (cardToken). An unknown
order responds 404 with a ProblemDetails whose detail is order-not-found:<id>. Otherwise charge
the order's total through ShopData.Charge: a declined charge responds 402 with a ProblemDetails
whose detail is payment-declined and the order is not marked paid; a successful charge marks
the order paid through ShopData.MarkOrderPaid and responds 200 whose body is the
PaymentAccepted JSON (paid: true).

Every problem response uses the framework problem machinery with content type
application/problem+json and the detail stated above; title, type, instance, and extensions
are not constrained.

Requirements:

- Correctness under the provided tests is the acceptance bar; the tests are visible to you.
- Keep the failure, absence, and accumulation semantics explicit and readable at the endpoints:
  a maintainer should see at a glance which rule failed, which status it maps to, and why.
- Do not read or copy the FunnySharp repository source code.

## Style: FunnySharp

Use the FunnySharp packages (already referenced) for absence, failure, and validation outcomes
where it fits, and ordinary C# everywhere else:

- Option<T> for absence (a missing product, a missing order),
- Validation<TValue, TError> for the accumulated validation checks,
- Result<TValue, TError> / UnitResult<TError> for fail-fast value-producing and command steps,
- ordinary exceptions stay exceptions.

You may read the FunnySharp package documentation in this repository: README.md and every file
under docs/ (option.md, result.md, unit-result.md, validation.md, grammar.md, analyzers.md,
function-composition.md, aspnet-core.md, and so on). The package ships compiler analyzers;
compiler diagnostics with FS ids report API misuse. Do not read or copy the FunnySharp source
code under src/.

The FunnySharp.AspNetCore package adds the Minimal API outcome-mapping extensions: ToHttpResult
and ToHttpResultAsync map Option<T>, Result<TValue, TError>, UnitResult<TError>, and
Validation<TValue, TError> (and their Task and ValueTask forms) to IResult at the endpoint
boundary, with explicit problem and success mappers. You may use them for the HTTP mapping and
keep the FunnySharp carriers in the domain.

## Delivery

Write every source file you need into the solution/ directory next to this prompt. Only files
under solution/ are copied into the build; the tests and contract are fixed. Compile with:

    dotnet fsi build.fsx -- -p eval-verify --task aspnetcore --style `STYLE` --run-dir <run-directory>

where <run-directory> contains your solution/ folder. A verifier reply with compiler errors,
test failures, or analyzer diagnostics is feedback: fix your code and verify again.
