# Фінальна перевірка ЛР2

Початок (час із TRX): 2026-10-19T20:50:52.5720995+03:00.
Завершення: 2026-10-19T20:50:56.1261819+03:00.

54 успішних, 0 невдалих, 0 пропущених. Release, WebApplicationFactory, локальна PostgreSQL.

11 наявних/scaffold випадків, 26 випадків контракту створення, 17 випадків пошуку.

Результати отримані з фактичного TRX; повні журнали SQL не включені.

| Тест | Результат | Тривалість |
|---|---|---|
| SecureLab.Api.Tests.IncidentCreationTests.T01_MaximumLengthsAndNearFutureDate_AreAccepted | Passed | 00:00:00.0125801 |
| SecureLab.Api.Tests.IncidentCreationTests.T01_ValidDto_IgnoresServerFieldsAndNormalizesUtc | Passed | 00:00:00.0671242 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "blank-description", field: "description") | Passed | 00:00:00.0047002 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "blank-title", field: "title") | Passed | 00:00:00.0046499 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "description-length-before-trim", field: "description") | Passed | 00:00:00.0048283 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "future-date", field: "occurredAtUtc") | Passed | 00:00:00.0139454 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "long-description", field: "description") | Passed | 00:00:00.0055538 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "long-title", field: "title") | Passed | 00:00:00.0046888 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "missing-date", field: "occurredAtUtc") | Passed | 00:00:00.0059376 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "missing-description", field: "description") | Passed | 00:00:00.0054685 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "missing-severity", field: "severity") | Passed | 00:00:00.0050919 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "missing-title", field: "title") | Passed | 00:00:00.0055227 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "numeric-severity", field: "severity") | Passed | 00:00:00.0051388 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "title-length-before-trim", field: "title") | Passed | 00:00:00.0046480 |
| SecureLab.Api.Tests.IncidentCreationTests.T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(scenario: "unknown-severity", field: "severity") | Passed | 00:00:00.0048336 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_ClosedOrDifferentCase_AllowsCreation(closed: False) | Passed | 00:00:00.0150068 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_ClosedOrDifferentCase_AllowsCreation(closed: True) | Passed | 00:00:00.0198395 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(status: "InProgress") | Passed | 00:00:00.0164783 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(status: "New") | Passed | 00:00:00.0174236 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(status: "Resolved") | Passed | 00:00:00.1317138 |
| SecureLab.Api.Tests.IncidentCreationTests.T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(status: "Triaged") | Passed | 00:00:00.0175659 |
| SecureLab.Api.Tests.IncidentCreationTests.T06_UnknownIncident_ReturnsSafe404 | Passed | 00:00:00.0551941 |
| SecureLab.Api.Tests.IncidentCreationTests.T09_DescriptionAfterTrim_Uses40CharacterBoundary(severity: "Critical", length: 39, valid: False) | Passed | 00:00:00.0039786 |
| SecureLab.Api.Tests.IncidentCreationTests.T09_DescriptionAfterTrim_Uses40CharacterBoundary(severity: "Critical", length: 40, valid: True) | Passed | 00:00:00.0140344 |
| SecureLab.Api.Tests.IncidentCreationTests.T09_DescriptionAfterTrim_Uses40CharacterBoundary(severity: "High", length: 39, valid: False) | Passed | 00:00:00.0052914 |
| SecureLab.Api.Tests.IncidentCreationTests.T09_DescriptionAfterTrim_Uses40CharacterBoundary(severity: "High", length: 40, valid: True) | Passed | 00:00:00.0105605 |
| SecureLab.Api.Tests.IncidentEndpointTests.ClientScript_DoesNotUseDangerousInnerHtmlSink | Passed | 00:00:00.0076200 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetDetails_DoesNotExposeInternalOwnerFields | Passed | 00:00:00.0068484 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetDetails_ForUnknownId_ReturnsProblemDetails404 | Passed | 00:00:00.0047069 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetList_ReturnsSeededIncidents | Passed | 00:00:00.0515072 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ForUnsupportedStatus_ReturnsValidationProblem(status: "") | Passed | 00:00:00.0014097 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ForUnsupportedStatus_ReturnsValidationProblem(status: "1") | Passed | 00:00:00.0028294 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ForUnsupportedStatus_ReturnsValidationProblem(status: "Unknown") | Passed | 00:00:00.0012856 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ReturnsOnlyOrderedAggregateFields(query: "", expectedSeverities: "Low,Medium,High,Critical", expectedCounts: "3,1,1,0") | Passed | 00:00:00.0086921 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ReturnsOnlyOrderedAggregateFields(query: "?status=Resolved", expectedSeverities: "Low,Medium,High,Critical", expectedCounts: "0,0,0,0") | Passed | 00:00:00.0037091 |
| SecureLab.Api.Tests.IncidentEndpointTests.GetSeveritySummary_ReturnsOnlyOrderedAggregateFields(query: "?status=Triaged", expectedSeverities: "Low,Medium,High,Critical", expectedCounts: "0,1,0,0") | Passed | 00:00:00.0382174 |
| SecureLab.Api.Tests.IncidentSearchTests.S02_OriginalControlInput_ReturnsNoUnrelatedRecords | Passed | 00:00:00.0053680 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_Metacharacters_AreLiteralInTitleAndDescription(literal: "%") | Passed | 00:00:00.0175589 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_Metacharacters_AreLiteralInTitleAndDescription(literal: "\\") | Passed | 00:00:00.0257398 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_Metacharacters_AreLiteralInTitleAndDescription(literal: "\\%_") | Passed | 00:00:00.0184717 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_Metacharacters_AreLiteralInTitleAndDescription(literal: "_") | Passed | 00:00:00.0235420 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_SemanticOrderThenId_IsAppliedBeforeTake50(sortBy: "") | Passed | 00:00:00.0265462 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_SemanticOrderThenId_IsAppliedBeforeTake50(sortBy: "createdAtUtc") | Passed | 00:00:00.0227185 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_SemanticOrderThenId_IsAppliedBeforeTake50(sortBy: "severity") | Passed | 00:00:00.2139724 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_SemanticOrderThenId_IsAppliedBeforeTake50(sortBy: "status") | Passed | 00:00:00.0296652 |
| SecureLab.Api.Tests.IncidentSearchTests.Search_SemanticOrderThenId_IsAppliedBeforeTake50(sortBy: null) | Passed | 00:00:00.0282471 |
| SecureLab.Api.Tests.IncidentSearchTests.T04_LegitimateSearch_ReturnsExactSeedMatch(q: "O'Brien", expectedId: "20000000-0000-0000-0000-000000000004") | Passed | 00:00:01.6131792 |
| SecureLab.Api.Tests.IncidentSearchTests.T04_LegitimateSearch_ReturnsExactSeedMatch(q: "USB", expectedId: "20000000-0000-0000-0000-000000000005") | Passed | 00:00:00.0074996 |
| SecureLab.Api.Tests.IncidentSearchTests.T04_LegitimateSearch_ReturnsExactSeedMatch(q: "usb", expectedId: "20000000-0000-0000-0000-000000000005") | Passed | 00:00:00.0034660 |
| SecureLab.Api.Tests.IncidentSearchTests.T04_LegitimateSearch_ReturnsExactSeedMatch(q: "комп'ютерного", expectedId: "20000000-0000-0000-0000-000000000003") | Passed | 00:00:00.0150745 |
| SecureLab.Api.Tests.IncidentSearchTests.T05_UnknownSort_ReturnsSafe400(sortBy: " ") | Passed | 00:00:00.0018350 |
| SecureLab.Api.Tests.IncidentSearchTests.T05_UnknownSort_ReturnsSafe400(sortBy: "Severity") | Passed | 00:00:00.0011833 |
| SecureLab.Api.Tests.IncidentSearchTests.T05_UnknownSort_ReturnsSafe400(sortBy: "price") | Passed | 00:00:00.0179220 |
| SecureLab.Api.Tests.SearchMechanicsTests.Search_ReturnsJson | Passed | 00:00:00.0662525 |
