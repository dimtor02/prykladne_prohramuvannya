# Перевірка контракту ЛР2

Початок (час із TRX): 2026-10-19T18:15:11.6960610+03:00.
Завершення: 2026-10-19T18:15:15.6871492+03:00.

37 успішних, 0 невдалих, 0 пропущених. Release, WebApplicationFactory, локальна PostgreSQL.

Результати отримані з фактичного TRX; повні журнали SQL не включені.

| Тест | Результат | Тривалість |
|---|---|---|
| SecureLab.Api.Tests.IncidentCreationTests.T01_MaximumLengthsAndNearFutureDate_AreAccepted | Passed | 00:00:00.0146406 |
| SecureLab.Api.Tests.IncidentCreationTests.T01_ValidDto_IgnoresServerFieldsAndNormalizesUtc | Passed | 00:00:00.0654289 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "blank-description", field: "description") | Passed | 00:00:00.0064246 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "blank-title", field: "title") | Passed | 00:00:00.0056199 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "description-length-before-trim", field: "description") | Passed | 00:00:00.0193810 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "future-date", field: "occurredAtUtc") | Passed | 00:00:00.0116428 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "long-description", field: "description") | Passed | 00:00:00.0074700 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "long-title", field: "title") | Passed | 00:00:00.0064658 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "missing-date", field: "occurredAtUtc") | Passed | 00:00:00.0072859 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "missing-description", field: "description") | Passed | 00:00:00.0061399 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "missing-severity", field: "severity") | Passed | 00:00:00.0068261 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "missing-title", field: "title") | Passed | 00:00:00.0067427 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "numeric-severity", field: "severity") | Passed | 00:00:00.0061286 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "title-length-before-trim", field: "title") | Passed | 00:00:00.0064195 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "unknown-severity", field: "severity") | Passed | 00:00:00.0060027 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_ClosedOrDifferentCase_AllowsCreation(closed: False) | Passed | 00:00:00.0176428 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_ClosedOrDifferentCase_AllowsCreation(closed: True) | Passed | 00:00:00.0240248 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(status: "InProgress") | Passed | 00:00:00.0273560 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(status: "New") | Passed | 00:00:00.0240510 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(status: "Resolved") | Passed | 00:00:00.3360134 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(status: "Triaged") | Passed | 00:00:00.0238389 |
| SecureLab.Api.Tests.IncidentCreationTests.T06_UnknownIncident_ReturnsSafe404 | Passed | 00:00:00.0060992 |
| SecureLab.Api.Tests.IncidentCreationTests.T09_DescriptionAfterTrim_Uses40CharacterBoundary(severity: "Critical", length: 39, valid: False) | Passed | 00:00:00.0045882 |
| SecureLab.Api.Tests.IncidentCreationTests.T09_DescriptionAfterTrim_Uses40CharacterBoundary(severity: "Critical", length: 40, valid: True) | Passed | 00:00:00.0155495 |
| SecureLab.Api.Tests.IncidentCreationTests.T09_DescriptionAfterTrim_Uses40CharacterBoundary(severity: "High", length: 39, valid: False) | Passed | 00:00:00.0041518 |
| SecureLab.Api.Tests.IncidentCreationTests.T09_DescriptionAfterTrim_Uses40CharacterBoundary(severity: "High", length: 40, valid: True) | Passed | 00:00:00.0122565 |
| SecureLab.Api.Tests.IncidentEndpointTests.ClientScript_DoesNotUseDangerousInnerHtmlSink | Passed | 00:00:00.0107682 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetDetails_DoesNotExposeInternalOwnerFields | Passed | 00:00:00.0767049 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetDetails_ForUnknownId_ReturnsProblemDetails404 | Passed | 00:00:00.0067178 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetList_ReturnsSeededIncidents | Passed | 00:00:01.7820156 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ForUnsupportedStatus_ReturnsValidationProblem(status: "") | Passed | 00:00:00.0022925 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ForUnsupportedStatus_ReturnsValidationProblem(status: "1") | Passed | 00:00:00.0192835 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ForUnsupportedStatus_ReturnsValidationProblem(status: "Unknown") | Passed | 00:00:00.0016582 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ReturnsOnlyOrderedAggregateFields(query: "", expectedSeverities: "Low,Medium,High,Critical", expectedCounts: "3,1,1,0") | Passed | 00:00:00.0108038 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ReturnsOnlyOrderedAggregateFields(query: "?status=Resolved", expectedSeverities: "Low,Medium,High,Critical", expectedCounts: "0,0,0,0") | Passed | 00:00:00.0053399 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ReturnsOnlyOrderedAggregateFields(query: "?status=Triaged", expectedSeverities: "Low,Medium,High,Critical", expectedCounts: "0,1,0,0") | Passed | 00:00:00.0914223 |
| SecureLab.Api.Tests.SearchMechanicsTests.Search_ReturnsJson | Passed | 00:00:00.1013723 |
