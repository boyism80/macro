def guild_donation(app):
	app.target.KeyPress(('ALT', 'U'))

	found = app.Detect(('guild-donation', 'attendance-reward-info', 'guild-create'), 0.9)
	if 'guild-create' in found:
		app.target.Escape()
		return

	if 'attendance-reward-info' in found:
		app.target.Escape()
		app.Sleep(500)

	app.target.Click(found['guild-donation']['position'])

	found = app.Detect(('donation', 'donation-disabled'), 0.7, {'x': 600, 'y': 550, 'width': 180, 'height': 50})
	if 'donation' in found:
		app.target.Click(found['donation']['position'])
		app.Sleep(500)
		app.target.Escape()
		app.Sleep(500)

	app.target.Escape()

	found = app.Detect('research-support', untilDetect=False)
	if 'research-support' in found:
		app.target.Click(found['research-support']['position'])

		app.Detect('research-support-main')
		found = app.Detect('cannot-support', area={'x': 700, 'y': 500, 'width': 200, 'height': 100}, untilDetect=False)
		if 'cannot-support' in found:
			app.target.Escape()
		else:
			app.target.Click((825, 535))
			app.Sleep(250)
			app.target.Click((900, 755))
			app.Sleep(500)

	app.target.Escape()

def daily_homework_rotation(app):
	character_slots = [(860, 400), (1120, 400), (600, 520), (860, 400), (1120, 400)]
	for character_slot in character_slots:
		app.target.Escape()
		found = app.Detect('change-character')
		app.target.Click(found['change-character']['position'])
		app.target.Click(character_slot)
		app.Sleep(500)

		found = app.Detect(('connect-enabled', 'connect-disabled'))
		if 'connect-enabled' not in found:
			continue
		
		app.target.Click(found['connect-enabled']['position'])

		found = app.Detect('connect-confirm')
		app.target.Click(found['connect-confirm']['position'])

		found = app.Detect('event-info'5, {'x': 920, 'y': 180, 'width': 100, 'height': 25})
		app.target.Escape()

		guild_donation(app)

		epona(app)

def enter_pw(app, pw):
	sprite_names = []
	for c in str(pw):
		sprite_name = 'num-' + c
		sprite_names.append(sprite_name)
	sprite_names = tuple(set(sprite_names))

	found = app.Detect(sprite_names)
	for c in str(pw):
		sprite_name = 'num-' + c
		app.target.Click(found[sprite_name]['position'])

def buy_pheon(app):
	app.target.KeyPress(115) # F4
	found = app.Detect('convenience')
	app.target.Click(found['convenience']['position'])

	found = app.Detect('pheon')
	app.target.Click(found['pheon']['position'])

	found = app.Detect('buy')
	app.target.Click(found['buy']['position'])

	found = app.Detect('buy-step-1')
	app.target.Click(found['buy-step-1']['position'])

	found = app.Detect('buy-step-2')
	app.target.Click(found['buy-step-2']['position'])

	enter_pw(app, 557575)

	app.target.Click((985, 680))
	found = app.Detect('connect-confirm')
	app.target.Click(found['connect-confirm']['position'])

	found = app.Detect('accept-reward')
	app.target.Click(found['accept-reward']['position'])

	found = app.Detect('accept-reward-confirm')
	app.target.Click(found['accept-reward-confirm']['position'])

	found = app.Detect('connect-confirm')
	app.target.Click(found['connect-confirm']['position'])

	app.target.Escape()
	app.target.Escape()
	app.target.KeyPress('I')

	found = app.Detect('pheon-icon')
	app.target.RClick(found['pheon-icon']['position'], ('ALT',))
	app.target.Enter()
	app.target.Escape()

def dialog_until_finish(app):
	app.target.KeyPress('G')
	app.Detect('quest-dialog')
	while True:
		found = app.Detect(('quest-dialog', 'loa-talk'))
		if 'quest-dialog' not in found:
			break

		app.target.KeyPress('G')

def show_bifrost(app):
	found = app.Detect('bifrost')
	app.target.Click(found['bifrost']['position'])
	app.Detect('bifrost-main')

def epona_mokomoko_night_market(app):
	dialog_wait_ms = 5000

	app.Detect('moko-moko-night-market')
	dialog_until_finish(app)

	app.target.RClick((400, 925))
	app.Sleep(2000)

	app.target.KeyPress('G')
	app.Sleep(dialog_wait_ms)

	app.target.RClick((1050, 925))
	app.Sleep(1500)

	app.target.KeyPress('G')
	app.Sleep(dialog_wait_ms)

	app.target.RClick((1380, 775))
	app.Sleep(1500)

	app.target.KeyPress('G')
	app.Sleep(dialog_wait_ms)

	app.target.RClick((1000, 100))
	app.Sleep(2000)

	app.target.RClick((980, 300))
	app.Sleep(1500)
	dialog_until_finish(app)

def epona_dark_forest(app):
	app.Detect('dark-forest')
	app.target.KeyPress('G')
	app.Sleep(4000)

	app.target.RClick((1200, 940))
	app.Sleep(2000)
	app.target.RClick((900, 915))
	app.Sleep(1500)

	app.target.KeyPress('G')
	app.Sleep(4000)

def epona(app):
	app.target.KeyPress(('ALT', 'J'))

	found = app.Detect('epona-main')

	app.target.Click((425, 240))
	app.Sleep(500)
	app.target.Click((425, 350))

	app.target.Click((1285, 355))
	app.Sleep(500)
	app.target.Click((1285, 430))
	app.Sleep(500)
	app.target.Click((1285, 505))
	app.Sleep(500)
	app.target.Escape()

	app.target.KeyPress(('ALT', 'W'))
	app.target.Click((1470, 385))
	app.target.Enter()
	epona_mokomoko_night_market(app)

	# show_bifrost(app)
	# found = app.Detect('no-named-rift')
	# found['no-named-rift']['position'] = (found['no-named-rift']['position'][0] + 300, found['no-named-rift']['position'][1])
	# app.target.Click(found['no-named-rift']['position'])
	# app.target.Enter()

	# epona_dark_forest(app)

def show_remote_post(app):
	app.target.KeyPress(('ALT', '`'))
	found = app.Detect('remote-post')
	app.target.Click(found['remote-post']['position'])


# -*- coding: utf-8 -*-
def callback(app):
	# dialog_until_finish(app)

	# return daily_homework_rotation(app)
	# return buy_pheon(app)

	# app.target.KeyPress(('ALT', 'W'))
	# app.target.KeyPress(('ALT', 'U'))
	pass