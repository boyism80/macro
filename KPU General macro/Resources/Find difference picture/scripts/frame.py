def callback(vmodel, frame, parameter):
	try:
		if not vmodel.State['done']:
			return

		# Waiting for next detection
		if vmodel.State['detect completion']:
			return

		# Perfect or result sprite is detected
		if vmodel.Sprite['result'].MatchTo(frame, (390, 110), (250, 75)) is not None or vmodel.Sprite['perfect'].MatchTo(frame, (225, 280), (580, 110)) is not None:
			vmodel.AddHistory('Complete to click all difference partitions.')
			vmodel.State['detect completion'] = True
			vmodel.SetTimer('detect', 4000, 'detect.py')
		else:
			vmodel.SetTimer('detect', 2000, 'detect.py')
			vmodel.AddHistory('Unknown error')
	except Exception as e:
		# vmodel.History = str(e)
		vmodel.AddHistory('frame.py excepion : ' + str(e))
		pass