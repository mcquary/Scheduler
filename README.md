# Scheduler
An algorithm for generating date sets that correspond to recurrence rules very similar to the style of Microsoft Outlook recurrance

Allows for creation of recurrance rules that can be stored in place of discrete sets of dates. The algorithm parses the rule and generates a set of dates to return that fits the rule. The rule options cover most things available in the Microsoft Outlook recurrance scheduling options.

Currently supports only dates. Any times applied to the EffectiveDate will be applied to the generated dates, but currently isn't checked for that level of accuracy. 
