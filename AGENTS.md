# AGENTS.md

This file provides guidance to coding agents when working with this repository.

## Testing

- Test naming: `Method_Should_Outcome_When_Condition`.
- Use the AAA section marker style. Each section starts with a single-line comment, and the test name documents intent:

  ```csharp
  // arrange
  ... arrange code ...

  // act
  ... act code ...

  // assert
  ... assert code ...
  ```
