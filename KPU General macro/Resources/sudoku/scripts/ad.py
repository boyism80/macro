def callback(vmodel, frame, parameter):
	try:
		vmodel.App.Click(parameter['close ad'])
		return parameter['close ad']
	except Exception as e:
		return str(e)