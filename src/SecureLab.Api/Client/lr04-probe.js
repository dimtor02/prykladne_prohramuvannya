    // Equivalent to the methodical guide's Console probe. The server still decides access.
    const tracker = document.querySelector('#tracker');
    document.querySelector('#own').addEventListener('click', () =>
      tracker.contentWindow.loadIncidentDetails('20000000-0000-0000-0000-000000000001'));
    document.querySelector('#foreign').addEventListener('click', () =>
      tracker.contentWindow.loadIncidentDetails('20000000-0000-0000-0000-000000000002'));
