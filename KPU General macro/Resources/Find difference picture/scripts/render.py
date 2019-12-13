def callback(vmodel, frame, parameter):
	try:
		for detection_area_list in vmodel.State['detection area list couple']:
			vmodel.DrawRectangles(frame, detection_area_list)
	except:
		pass