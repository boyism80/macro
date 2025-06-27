# macro

General purpose macro that works screen capture based. Write python script to detect and do something to target process simply. You can manage screen capture, edit resource in this program.

## How to works?
Update latest frame of target process and execute python script. Here is sample code.
```python
def callback(app):
	app.KeyPress(('ALT', 'U'))

	found = app.Detect(('guild-donation', 'attendance-reward-info', 'guild-create'), 0.9)
	if 'guild-create' in found:
		app.Escape()
		return

	if 'attendance-reward-info' in found:
		app.Escape()
		app.Sleep(500)

	app.Click(found['guild-donation']['position'])

	found = app.Detect(('donation', 'donation-disabled'), 0.7, {'x': 600, 'y': 550, 'width': 180, 'height': 50})
	if 'donation' in found:
		app.Click(found['donation']['position'])
		app.Sleep(500)
		app.Escape()
		app.Sleep(500)

	app.Escape()
```

You can detect using ```app.Detect``` method. the last parameter is factor value of image resource. The higher this value, the more accurate it is, but resources may not be detected. Default is 0.8.
When this method call, this thread(not main thread) is blocked while detect resource and return detected information then you can do anything with the result. Also you can set timeout and speficied area.

## Sample

[Find difference between 2 pictures](https://youtu.be/8LgthrlfDYw)

[SUDOKU mobile](https://youtu.be/HhToEqJBmVU)

[Lost Ark auto fishing](https://youtu.be/Q3rxFIN1Uxs) 
 ([docs](https://blog.naver.com/boyism/221421048934))
