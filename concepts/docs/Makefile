.SUFFIXES: .md .pdf

all: linq-slides-2017-10-16.html tour.pdf compare.pdf

.md.pdf:
	pandoc -f markdown -t latex -s $< -o $@

linq-slides-2017-10-16.html: linq-slides-2017-10-16.md
	pandoc -f markdown -t slidy -s linq-slides-2017-10-16.md -o linq-slides-2017-10-16.html