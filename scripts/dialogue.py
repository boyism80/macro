def dialog_until_finish(app):
	app.KeyPress('G')
	app.Detect('quest-dialog')
	while True:
		found = app.Detect(('quest-dialog', 'quest-complete', 'loa-talk'))
		if 'quest-complete' in found:
			app.Escape()
			break

		if 'quest-dialog' not in found:
			break

		app.KeyPress(('SHIFT', 'G'))
			

def callback(app):
	return dialog_until_finish(app)