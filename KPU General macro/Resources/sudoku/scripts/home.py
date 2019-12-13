def callback(vmodel, frame, parameter):
	try:
		# vmodel.app.Click(parameter['search box'])
		# return True

		location = vmodel.Sprite['sudoku icon'].MatchTo(frame)
		vmodel.App.Click(location)
	except Exception as e:
		return str(e)