def callback(vmodel, frame, parameter):
	try:
		location = vmodel.Sprite['expert'].MatchTo(frame)
		vmodel.App.Click(location)
		return 'choose expert level'
	except Exception as e:
		return str(e)