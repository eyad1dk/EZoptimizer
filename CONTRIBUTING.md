# Contributing

Keep changes small, explain the problem, and include the checks you ran.

For a new tuning option, include a documented Windows API, original-value capture, an undo path, read-back verification, and a clear explanation of the tradeoff. Don't add unexplained registry packs or performance claims without workload measurements.

The app should run without elevation. Windows restrictions should produce a useful error and leave enough history to recover.

Run the regression suite and build commands from the README before submitting a pull request. Check the interface at the minimum window size and with display scaling enabled when changing layout.

Do not include personal system reports, compiled binaries or local history files in a pull request.
