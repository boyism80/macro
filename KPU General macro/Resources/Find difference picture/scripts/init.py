import detect

def callback(vmodel, frame, parameter):
	vmodel.State['detect completion'] = False
	vmodel.State['detection area list couple'] = None
	vmodel.State['done'] = False

	return detect.callback(vmodel, None, None)