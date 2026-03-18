package io.github.ldev22.utils;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.graphics.GL20;
import com.badlogic.gdx.graphics.g2d.Batch;

import static jdk.internal.misc.Blocker.begin;
import static sun.jvm.hotspot.runtime.PerfMemory.end;

public class GdxGraphics {
    public static void clearScreen(Color color){
        color = Color.BLACK;
        Gdx.gl.glClearColor(0f, 0f, 1f, 1f);
        Gdx.gl.glClear(GL20.GL_COLOR_BUFFER_BIT);
    }

    public static void clearScreen(float red, float green, float blue, float alpha){
        Gdx.gl.glClearColor(0f, 0f, 1f, 1f);
        Gdx.gl.glClear(GL20.GL_COLOR_BUFFER_BIT);
    }

    public static void use(Batch batch, Runnable action) {
        batch.begin();
        action.run();
        batch.end();
    }
}

/*@JvmOverload
fun Viewport.drawGrid(renderer : ShapeRenderer, cellSize : Int = 1){
	val oldColor = renderer.color.cpy()
	val doubleWorldWidth = worldWidth * 2
	val doubleWorldHeight = worldHeight * 2

	renderer.use{
		renderer.color = Color.WHITE
		//draw vertical lines

		var x = -doubleWorldWidth
		while(x < doubleWorldWidth){
			renderer.line(x - doubleWorldHeight, x, doubleWorldHeight)
			x += cellSize
		}

		var y = -doubleWorldHeight
		while(y < doubleWorldHeight){
			renderer.line(-doubleWorldWidth, y, doubleWorldWidth, y)
			y += cellSize
		}

		render.color = Color.RED
		renderer.line(0f, -doubleWorldHeight, 0f, doubleWorldHeight)

		renderer.line(-doubleWorldWidth, 0f, doubleWorldWidth, 0f)

		//world bounds
		render.color = Color.YELLOW
		renderer.line(0f, worldHeight, worldWidth, worldHeight)
		renderer.line(worldWidth, 0f, worldWidth, worldHeight)
	}

	renderer.color = oldColor
} */