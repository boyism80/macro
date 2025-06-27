def callback(app):
	found = app.Detect('forest-watchman', timeout=0)
	if 'forest-watchman' not in found:
		app.KeyPress('G')
	else:
		app.KeyPress(116)
		app.KeyPress(116)
