all: pdf

pdf: ps
	ps2pdf progress_report.ps
	rm -f progress_report.aux
	rm -f progress_report.dvi
	rm -f progress_report.log
	rm -f progress_report.ps
	rm -f progress_report.tox
	
ps: dvi
	dvips -Ppdf -G0 progress_report.dvi
	
dvi:
	latex progress_report.tex
	
clean:
	rm -f progress_report.aux
	rm -f progress_report.dvi
	rm -f progress_report.log
	rm -f progress_report.ps
	rm -f progress_report.tox
	rm -f progress_report.pdf