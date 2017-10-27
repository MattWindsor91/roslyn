.SUFFIXES: .md .pdf .html
.PHONY: all pdfs slides

all: pdfs slides

pdfs: \
	compare.pdf \
	issues.pdf \
	features/autofilling.pdf \
	tour.pdf

slides: \
	linq-slides-2017-10-16.html

.md.pdf:
	pandoc -f markdown -t latex -s $< -o $@

.md.html:
	pandoc -f markdown -t slidy -s $< -o $@
