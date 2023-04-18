def callback(app):
	found = app.Detect('forest-watchman', untilDetect=False)
	if 'forest-watchman' not in found:
		app.target.KeyPress('G')
	else:
		app.target.KeyPress(116)
		app.target.KeyPress(116)
