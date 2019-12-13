def callback(vmodel, frame, parameter):
	try:
		vmodel.App.Click(parameter['skip ad'])
		return parameter['close ad']
	except Exception as e:
		return str(e)