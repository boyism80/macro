def dialog_until_finish(app):
	app.target.KeyPress('G')
	app.Detect('quest-dialog')
	while True:
		found = app.Detect(('quest-dialog', 'quest-complete', 'loa-talk'))
		if 'quest-complete' in found:
			app.target.Escape()
			break

		if 'quest-dialog' not in found:
			break

		app.target.KeyPress(('SHIFT', 'G'))
			

def callback(app):
	return dialog_until_finish(app)