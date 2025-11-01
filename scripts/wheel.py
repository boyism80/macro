import stone

def receive_epona(app):
	app.KeyPress(('ALT', 'J'))

	found = app.Detect('epona-main')

	app.Click((425, 240))
	app.Sleep(500)
	app.Click((425, 350))

	app.Click((1285, 355))
	app.Sleep(500)
	app.Click((1285, 430))
	app.Sleep(500)
	app.Click((1285, 505))
	app.Sleep(500)
	app.Escape()

def bifrost_1(app):
	app.KeyPress(('ALT', 'W'))
	app.Detect('bifrost-main')
	app.Click((1470, 385))
	app.Enter()
	app.Sleep(1000)

	app.Detect('loa-talk')

def fishing(app, mini_game=True):
	pivot = app.GetCursorPosition()
	while True:
		found = app.Detect('gear-crash', timeout=0)
		if found:
			app.KeyPress(('ALT', '`'))
			
			found = app.Detect('pet-repair-icon')
			app.Click(found['pet-repair-icon']['position'])

			found = app.Detect('repair-all')
			app.Click(found['repair-all']['position'])

			app.Enter()
			app.Escape()
			app.Escape()

		found = app.Detect('fishing-buff', area={"x": 696, "y": 1017, "width": 202, "height": 51}, timeout=0)
		if found:
			app.KeyPress('D')
			app.Sleep(2500)

		prev = app.SetCursorPosition(pivot)
		app.KeyPress('W')
		app.Sleep(1000)
		app.SetCursorPosition(prev)

		found = app.Detect('not-enough-energy', area={"x":823,"y":749,"width":270,"height":75}, timeout=1000*3)
		if 'not-enough-energy' in found:
			app.Click((1033, 927))

			# potions = ('life-energy-potion(large)', 'life-energy-potion(normal)', 'life-energy-potion(small)')
			potions = ('life-energy-potion(large)', 'life-energy-potion(normal)')
			found = app.Detect(potions, timeout=500)
			if not found:
				app.Escape()
				break

			if 'life-energy-potion(small)' in found:
				app.Click((900, 564))
			elif 'life-energy-potion(normal)' in found:
				app.Click((900, 640))
			elif 'life-energy-potion(large)' in found:
				app.Click((900, 713))

			app.Escape()
			continue

		candidates = ('fish-catch', 'fish-fail', 'fishing-mini-game') if mini_game else ('fish-catch', 'fish-fail')
		found = app.Detect(candidates, timeout=1000*30)
		if 'fish-catch' in found:
			app.KeyPress('W')
			app.Sleep(5200)
			if mini_game:
				app.KeyPress('F')
			app.Sleep(800)
		elif 'fish-fail' in found:
			yield 'failed'
			app.Sleep(2000)
		elif 'fishing-mini-game' in found:
			yield 'mini game start'
			app.Sleep(14000)
			yield 'mini game end'
		else:
			pass

def stone_simulate(app, engraving_name=None):
	index = 0
	app.KeyPress('G')
	while True:
		exists = stone.exists(app, index)
		if not exists:
			break

		if stone.facet_stone(app, index, engraving_name):
			yield 'facet stone success'
			return

		index = index + 1
		if index >= 12:
			found = app.Detect(('enable_next_stone', 'disable_next_stone'), area={"x":611,"y":759,"width":46,"height":34})
			if 'enable_next_stone' in found:
				index = 11
				app.Click(631, 778)

			elif 'disable_next_stone' in found:
				yield 'disable next stone'
				break
			else:
				yield 'unexpected exception'
				return


	yield 'facet stone end'
	app.Escape()
	stone.disassemble_stones(app)

def callback(app):
	# return fishing(app, mini_game=False)

	stone_simulate(app)