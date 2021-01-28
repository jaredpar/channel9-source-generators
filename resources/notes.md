
There is so much code generation going on today 


Existing approaches 
    - Code fixers
        - Manual
        - Have to get users to take action
        - Tied to the editor 
        - Only as good as the analyzer
    - Analyzers
        - Can spot what you're doing wrong 
        - Can be tricky though. 
        - Easy to fool an analyzer that is making sure that you use every member

Source generators
    - Have the full power of the Roslyn API
        - Look at all the features it supports
        - Just like analyzers, generators have the full Roslyn API at their disposal.

