from base_fishing import *

def callback(vmodel, frame, parameter):
	try:
		if detect_catch(vmodel, frame) is not None and pull_fish_rod(vmodel):
			return 'Press \'W\' to pull fishing rod'

		elif detect_reduce(vmodel, frame) is not None and wait_next_action(vmodel):
			return 'Press \'W\' to catch fish'

	except Exception as e:
		return str(e)