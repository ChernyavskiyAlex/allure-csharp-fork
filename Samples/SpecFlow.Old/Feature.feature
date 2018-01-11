Feature: Feature
	In order to avoid silly mistakes
	As a math idiot
	I want to be told the sum of two numbers

@Simple
Scenario: Add two numbers
	Given I have entered 50 into the calculator
	And I have entered 70 into the calculator
	When I press add
	Then the result should be 120 on the screen

@Outline
Scenario Outline: Add two numbers outline
	Given I have entered <r> into the calculator
	And I have entered <r> into the calculator
	When I press add
	Then the result should be <r> on the screen
	Examples: 
	| f | s | r |
	| 1 | 2 | 3 |
	| 2 | 3 | 5 |
