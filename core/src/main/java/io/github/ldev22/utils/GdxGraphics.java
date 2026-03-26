package io.github.ldev22.utils;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.graphics.GL20;
import com.badlogic.gdx.graphics.g2d.Batch;
import com.badlogic.gdx.graphics.glutils.ShapeRenderer;
import com.badlogic.gdx.utils.viewport.Viewport;

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


    public static void drawGrid(Viewport viewport, ShapeRenderer renderer, int cellSize){
        Color oldColor = new Color(renderer.getColor());

        float worldWidth = viewport.getWorldWidth();
        float worldHeight = viewport.getWorldHeight();

        float doubleWorldWidth = worldWidth * 2;
        float doubleWorldHeight = worldHeight * 2;

        renderer.begin(ShapeRenderer.ShapeType.Line);
        renderer.setColor(Color.BLACK);
        //vertical lines
        for(float x = -doubleWorldWidth; x < doubleWorldWidth; x += cellSize){
            renderer.setColor(Color.WHITE);
            renderer.line(x, -doubleWorldHeight, x, doubleWorldHeight);
        }

        //horizontal lines
        for(float y = -doubleWorldHeight; y < doubleWorldHeight; y+= cellSize){
            renderer.setColor(Color.WHITE);
            renderer.line(-doubleWorldWidth, y, doubleWorldWidth, y);
        }

        //axis lines(red)
        renderer.setColor(Color.RED);

        renderer.line(0f, -doubleWorldHeight, 0f, doubleWorldHeight);
        renderer.line(-doubleWorldWidth, 0f, doubleWorldWidth, 0f);

        //world bounds
        renderer.setColor(Color.YELLOW);
        renderer.line(0f, worldHeight, worldWidth, worldHeight);
        renderer.line(worldWidth, 0f, worldWidth, worldHeight);

        renderer.end();

        renderer.setColor(oldColor);
    }
}
