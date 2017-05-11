set FilterClause=--where "cat != PNL_Domain && cat != Long_Running"
set NUnitExe="C:\Program Files (x86)\NUnit.org\nunit-console\nunit3-console.exe"

%NUnitExe% ..\Test\bin\Debug\Test.dll %FilterClause% --out=NUnitConsole.txt --result:NUnit_UnitTests.xml --timeout=90000

