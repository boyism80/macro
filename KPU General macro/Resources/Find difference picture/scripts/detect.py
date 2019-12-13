import click_detection

def callback(vmodel, frame, parameter):
	vmodel.State['done'] = False
	try:
		vmodel.State['detect completion'] = False
		vmodel.SourceFrameLock.WaitOne()
		frame = vmodel.SourceFrame
		if frame is None:
			raise Exception()

		# if detector has one or more location that is not clicked
		if vmodel.State['detection area list couple'] is not None:
			raise Exception('Detection area list is empty')
		
		voffset1        = (142, 26)
		voffset2        = (518, 26)
		vsize           = (365, 616)
		hoffset1        = (142, 26)
		hoffset2        = (142, 340)
		hsize           = (740, 301)

		vmodel.State['detection area list couple'] = vmodel.Compare(frame, voffset1, voffset2, vsize) or vmodel.Compare(frame, hoffset1, hoffset2, hsize)

		# if detector cannot find any difference partition.
		if vmodel.State['detection area list couple'] is None:
			raise Exception()
			# raise Exception('cannot find any difference partition.')

		# if detector found some difference partitions.
		else:
			vmodel.AddHistory('Try to detect difference partition.')
			return click_detection.callback(vmodel, None, None)
	except Exception as e:
		vmodel.SetTimer('detect', 500, 'detect.py')

	finally:
		vmodel.SourceFrameLock.ReleaseMutex()