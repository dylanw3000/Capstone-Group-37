all:
	latex abstract.tex
	dvips abstract.dvi -o abstract.ps
	ps2pdf abstract.ps abstract.pdf