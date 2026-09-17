#!/usr/bin/env python3
"""Batch raw-LOC report for the call-site scratch files."""
import sys
from loc import extract

TARGETS = [
    ("W1  dictionary+fallback", "idiomatic/Workflows.cs", ["TimeoutSeconds"]),
    ("W1  dictionary+fallback", "funnysharp/Workflows.cs", ["TimeoutSeconds"]),
    ("W1  dictionary+fallback", "competitors/W1Funcky.cs", ["TimeoutSeconds"]),
    ("W2  fail-fast pipeline", "idiomatic/Workflows.cs", ["CreateInvoiceAsync"]),
    ("W2  fail-fast pipeline", "funnysharp/Workflows.cs", ["CreateInvoiceAsync", "FindCustomerAsync", "InvalidQuantityFor"]),
    ("W2  fail-fast pipeline", "competitors/W2Cfe.cs", ["CreateInvoiceAsync", "InvalidQuantityFor"]),
    ("W3  accumulate fields", "idiomatic/Workflows.cs", ["ValidateSignup"]),
    ("W3  accumulate fields", "funnysharp/Workflows.cs", ["ValidateSignup", "ValidateEmail", "ValidatePassword", "ValidateAge"]),
    ("W3  accumulate fields", "competitors/W3LanguageExt.cs", ["ValidateSignup", "ValidateEmail", "ValidatePassword", "ValidateAge"]),
    ("W3b accumulate fields (CFE)", "competitors/W3bCfe.cs", ["ValidateSignup", "ValidateEmail", "ValidatePassword", "ValidateAge"]),
    ("W4  unit-result", "idiomatic/Workflows.cs", ["DeleteOrderAsync"]),
    ("W4  unit-result", "funnysharp/Workflows.cs", ["DeleteOrderAsync"]),
    ("W4  unit-result", "competitors/W4Cfe.cs", ["DeleteOrderAsync"]),
    ("W5  tap/observation", "idiomatic/Workflows.cs", ["TotalWithAudit"]),
    ("W5  tap/observation", "funnysharp/Workflows.cs", ["TotalWithAudit"]),
    ("W5  tap/observation", "competitors/W5Cfe.cs", ["TotalWithAudit"]),
    ("W6  traversal+index", "idiomatic/Workflows.cs", ["ParseAll", "ParseLine"]),
    ("W6  traversal+index", "funnysharp/Workflows.cs", ["ParseAll", "ParseLine"]),
    ("W7  bounded parallel (op+consumer)", "idiomatic/Workflows.cs", ["SelectParallelAsync", "SumFetchedAsync"]),
    ("W7  bounded parallel (consumer)", "funnysharp/Workflows.cs", ["SumFetchedAsync"]),
    ("W8  first success", "idiomatic/Workflows.cs", ["FirstInvoiceAsync"]),
    ("W8  first success", "funnysharp/Workflows.cs", ["FirstInvoiceAsync"]),
    ("W9  env+resource", "idiomatic/Workflows.cs", ["LoadOrderTotalAsync"]),
    ("W9  env+resource", "funnysharp/Workflows.cs", ["LoadOrderTotalAsync"]),
    ("W10 nested update", "idiomatic/Workflows.cs", ["ReviewPostalCode"]),
    ("W10 nested update", "funnysharp/Workflows.cs", ["ReviewPostalCode"]),
    ("W10 nested update", "competitors/W10LanguageExt.cs", ["ReviewPostalCode"]),
    ("W11 http idiomatic", "funnysharp-aspnet/Workflows.cs", ["GetCustomerIdiomatic", "CreateOrderIdiomaticAsync", "SignUpIdiomatic"]),
    ("W11 http funny", "funnysharp-aspnet/Workflows.cs", ["GetCustomer", "CreateOrder", "SignUp", "CreateOrderFromEffectAsync"]),
]

total = 0
for label, path, names in TARGETS:
    counts = []
    for name in names:
        r = extract(path, name)
        if r is None:
            counts.append(f"{name}=NOT_FOUND")
            continue
        counts.append(f"{name}={len(r[1])}")
    value = sum(len(extract(path, n)[1]) for n in names if extract(path, n))
    total += value
    print(f"{label:34s} {path:28s} raw={value:3d}  ({', '.join(counts)})")
print(f"TOTAL raw lines counted: {total}")
