(module
	(import "" "mem" (memory 1))
	(data (i32.const 0) "WASM!")
	(func $run
		(memory.copy (i32.add (i32.load8_u (i32.const 102)) (i32.const 102)) (i32.const 0) (i32.const 5))
		(i32.store8 (i32.const 102) (i32.add (i32.load8_u (i32.const 102)) (i32.const 5)))
		(i32.store8  (i32.const 100) (i32.add (i32.load8_u (i32.const 102)) (i32.const 2)))
	)
	(export "run" (func $run))
)