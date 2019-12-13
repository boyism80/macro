def callback(vmodel, frame, parameter):
	try:
		location = vmodel.Sprite['new game'].MatchTo(frame)
		vmodel.App.Click(location)
		return location
	except Exception as e:
		return str(e)