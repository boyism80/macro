def dialog_until_finish(app):
	app.target.KeyPress('G')
	app.Detect('quest-dialog-2')
	while True:
		found = app.Detect(('quest-dialog-2', 'quest-complete-2', 'loa-talk'))
		if 'quest-complete-2' in found:
			app.target.Escape()
			break

		if 'quest-dialog-2' not in found:
			break

		app.target.KeyPress(('SHIFT', 'G'))
			

def callback(app):
	return dialog_until_finish(app)