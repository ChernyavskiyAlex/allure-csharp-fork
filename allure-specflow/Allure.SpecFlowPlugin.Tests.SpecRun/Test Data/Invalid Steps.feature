﻿@ignore
Feature: Invalid Steps

@testdata
Scenario: All steps are invalid
	Given I have entered 50 into the calculator

@testdata
Scenario: Some steps are invalid
	Given Step is 'passed'
	Given I don't have such step
	Given Step is 'passed'
	And I don't have such step too

