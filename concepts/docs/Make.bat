call C:\nodejs\nodevars.bat
REM npm install markdown-to-html -g
REM markdown-to-slides -l "template\\template.html"  -i fsconcepts.md -o fsconcepts.html
markdown-to-slides -s "template\\style.css" -l "template\\template.html"  -i csconcepts.md -o csconcepts.html